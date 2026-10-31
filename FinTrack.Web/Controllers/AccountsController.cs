
using System.Security.Claims;
using FinTrack.Application.Accounts.DTOs;
using FinTrack.Application.Accounts.Interfaces;
using FinTrack.Web.ViewModels.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Web.Controllers;

[Authorize]
public class AccountsController : Controller
{
    private readonly IAccountService _accountService;

    public AccountsController(IAccountService accountService)
    {
        _accountService = accountService;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetAllAsync(
            CurrentUserId, cancellationToken);

        var archived = await _accountService.GetArchivedAsync(
            CurrentUserId, cancellationToken);

        ViewData["ArchivedCount"] = archived.Count;

        return View(accounts);
    }

    [HttpGet]
    public async Task<IActionResult> Archived(
        CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetArchivedAsync(
            CurrentUserId, cancellationToken);

        return View(accounts);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateAccountViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateAccountViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var request = new CreateAccountRequest(
                model.Name,
                model.Type,
                model.OpeningBalance,
                model.Currency);

            await _accountService.CreateAsync(
                CurrentUserId, request, cancellationToken);

            TempData["SuccessMessage"] =
                "Account created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        var account = await _accountService.GetByIdAsync(
            CurrentUserId, id, cancellationToken);

        if (account is null)
            return NotFound();

        var model = new EditAccountViewModel
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type,
            Currency = account.Currency
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditAccountViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var updated = await _accountService.UpdateAsync(
                CurrentUserId,
                id,
                new UpdateAccountRequest(
                    model.Name,
                    model.Type,
                    model.Currency),
                cancellationToken);

            if (!updated)
                return NotFound();

            TempData["SuccessMessage"] =
                "Account updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(
        int id,
        CancellationToken cancellationToken)
    {
        var archived = await _accountService.ArchiveAsync(
            CurrentUserId, id, cancellationToken);

        if (!archived)
            return NotFound();

        TempData["SuccessMessage"] =
            "Account archived. You can restore it from Archived accounts.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unarchive(
        int id,
        CancellationToken cancellationToken)
    {
        var restored = await _accountService.UnarchiveAsync(
            CurrentUserId, id, cancellationToken);

        if (!restored)
            return NotFound();

        TempData["SuccessMessage"] =
            "Account restored successfully.";

        return RedirectToAction(nameof(Index));
    }
}
