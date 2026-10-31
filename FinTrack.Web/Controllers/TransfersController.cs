
using System.Globalization;
using System.Security.Claims;
using FinTrack.Application.Accounts.Interfaces;
using FinTrack.Application.Common;
using FinTrack.Application.Transfers.DTOs;
using FinTrack.Application.Transfers.Interfaces;
using FinTrack.Web.ViewModels.Transfers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinTrack.Web.Controllers;

[Authorize]
public class TransfersController : Controller
{
    private readonly ITransferService _transferService;
    private readonly IAccountService _accountService;

    public TransfersController(
        ITransferService transferService,
        IAccountService accountService)
    {
        _transferService = transferService;
        _accountService = accountService;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var transfers = await _transferService.GetPagedAsync(
            CurrentUserId,
            page,
            PagedResult<TransferDto>.DefaultPageSize,
            cancellationToken);

        return View(transfers);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        int? fromAccountId,
        CancellationToken cancellationToken)
    {
        var model = new CreateTransferViewModel
        {
            FromAccountId = fromAccountId ?? 0
        };

        await PopulateAccountsAsync(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateTransferViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.FromAccountId > 0 && model.FromAccountId == model.ToAccountId)
        {
            ModelState.AddModelError(
                nameof(model.ToAccountId),
                "Choose a different account to transfer to.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _transferService.CreateAsync(
                    CurrentUserId,
                    new CreateTransferRequest(
                        model.FromAccountId,
                        model.ToAccountId,
                        model.Amount,
                        model.TransferDate,
                        model.Note),
                    cancellationToken);

                TempData["SuccessMessage"] =
                    "Transfer recorded successfully.";

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
        }

        await PopulateAccountsAsync(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        var deleted = await _transferService.SoftDeleteAsync(
            CurrentUserId,
            id,
            cancellationToken);

        if (!deleted)
            return NotFound();

        TempData["SuccessMessage"] =
            "Transfer deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateAccountsAsync(
        CreateTransferViewModel model,
        CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetAllAsync(
            CurrentUserId,
            cancellationToken);

        model.Accounts = accounts
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(CultureInfo.InvariantCulture),
                Text = $"{a.Name} · {a.CurrentBalance:N2} {a.Currency}"
            })
            .ToList();

        model.AccountCurrencies = accounts
            .ToDictionary(a => a.Id, a => a.Currency);
    }
}
