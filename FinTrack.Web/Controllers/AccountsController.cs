
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

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier)!;

        var accounts = await _accountService.GetAllAsync(
            userId, cancellationToken);

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

        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier)!;

        var request = new CreateAccountRequest(
            model.Name,
            model.Type,
            model.OpeningBalance);

        await _accountService.CreateAsync(
            userId, request, cancellationToken);

        TempData["SuccessMessage"] =
            "Account created successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier)!;

        var account = await _accountService.GetByIdAsync(
            userId, id, cancellationToken);

        if (account is null)
            return NotFound();

        var model = new EditAccountViewModel
        {
            Id = account.Id,
            Name = account.Name,
            Type = account.Type
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

        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier)!;

        var updated = await _accountService.UpdateAsync(
            userId,
            id,
            model.Name,
            model.Type,
            cancellationToken);

        if (!updated)
            return NotFound();

        TempData["SuccessMessage"] =
            "Account updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(
        int id,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier)!;

        var archived = await _accountService.ArchiveAsync(
            userId, id, cancellationToken);

        if (!archived)
            return NotFound();

        TempData["SuccessMessage"] =
            "Account archived successfully.";

        return RedirectToAction(nameof(Index));
    }


}
