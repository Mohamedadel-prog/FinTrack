
using System.Text;
using System.Text.Encodings.Web;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Infrastructure.Identity;
using FinTrack.Web.ViewModels.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace FinTrack.Web.Controllers;

public class AuthController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model,
        CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();

        var user = new ApplicationUser
        {
            FullName = model.FullName.Trim(),
            UserName = email,
            Email = email
        };

        var result = await _userManager.CreateAsync(
            user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(
                    string.Empty, error.Description);

            return View(model);
        }

        await SendConfirmationEmailAsync(user, cancellationToken);

        return RedirectToAction(
            nameof(RegisterConfirmation),
            new { email });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult RegisterConfirmation(string? email)
    {
        ViewData["Email"] = email;

        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(
        string? userId,
        string? code)
    {
        var confirmed = false;

        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(code))
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is not null && TryDecodeToken(code, out var token))
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);
                confirmed = result.Succeeded;
            }
        }

        ViewData["Confirmed"] = confirmed;

        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResendConfirmation(string? email)
    {
        return View(new ResendConfirmationViewModel
        {
            Email = email ?? string.Empty
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmation(
        ResendConfirmationViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(email);

        // Same response whether or not the address is registered,
        // so the form can't be used to discover accounts.
        if (user is not null && !user.EmailConfirmed)
            await SendConfirmationEmailAsync(user, cancellationToken);

        return RedirectToAction(
            nameof(RegisterConfirmation),
            new { email });
    }


    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;

        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Account temporarily locked. Please try again later.");

            return View(model);
        }

        // Identity reports "not allowed" before it checks the password, so
        // only mention the unconfirmed email once the password is proven.
        if (result.IsNotAllowed &&
            await _userManager.CheckPasswordAsync(user, model.Password))
        {
            ViewData["UnconfirmedEmail"] = email;

            ModelState.AddModelError(
                string.Empty,
                "Please confirm your email address before signing in.");

            return View(model);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                string.Empty,
                "Invalid email or password.");

            return View(model);
        }

        if (!string.IsNullOrEmpty(returnUrl)
            && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }


    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return RedirectToAction("Login", "Auth");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());

        // Always show the same confirmation page so the form can't be used
        // to find out which email addresses have an account.
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var link = Url.Action(
                nameof(ResetPassword),
                "Auth",
                new { email = user.Email, code = EncodeToken(token) },
                Request.Scheme)!;

            await _emailSender.SendAsync(
                user.Email!,
                "Reset your FinTrack password",
                $"""
                <p>Hi {HtmlEncoder.Default.Encode(user.FullName)},</p>
                <p>We received a request to reset your FinTrack password.
                <a href="{HtmlEncoder.Default.Encode(link)}">Choose a new password</a>.</p>
                <p>If you didn't ask for this, you can ignore this email.</p>
                """,
                cancellationToken);
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? code)
    {
        if (string.IsNullOrEmpty(code))
            return BadRequest();

        return View(new ResetPasswordViewModel
        {
            Email = email ?? string.Empty,
            Code = code
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());

        if (user is null || !TryDecodeToken(model.Code, out var token))
        {
            // Don't reveal whether the account exists
            ModelState.AddModelError(
                string.Empty,
                "This reset link is invalid or has expired.");

            return View(model);
        }

        var result = await _userManager.ResetPasswordAsync(
            user, token, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Code == nameof(IdentityErrorDescriber.InvalidToken)
                        ? "This reset link is invalid or has expired."
                        : error.Description);
            }

            return View(model);
        }

        // The reset link proved the user owns the address, and a lockout
        // from earlier failed attempts shouldn't block the new password.
        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        await _userManager.SetLockoutEndDateAsync(user, null);

        return RedirectToAction(nameof(ResetPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SendConfirmationEmailAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        var link = Url.Action(
            nameof(ConfirmEmail),
            "Auth",
            new { userId = user.Id, code = EncodeToken(token) },
            Request.Scheme)!;

        await _emailSender.SendAsync(
            user.Email!,
            "Confirm your FinTrack account",
            $"""
            <p>Hi {HtmlEncoder.Default.Encode(user.FullName)},</p>
            <p>Welcome to FinTrack! Please
            <a href="{HtmlEncoder.Default.Encode(link)}">confirm your email address</a>
            to start using your account.</p>
            """,
            cancellationToken);
    }

    // Identity tokens contain characters that don't survive a URL,
    // so they travel Base64Url-encoded.
    private static string EncodeToken(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static bool TryDecodeToken(string code, out string token)
    {
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            return true;
        }
        catch (FormatException)
        {
            token = string.Empty;
            return false;
        }
    }
}
