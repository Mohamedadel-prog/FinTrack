
using System.Globalization;
using System.Security.Claims;
using FinTrack.Application.Accounts.Interfaces;
using FinTrack.Application.Categories.Interfaces;
using FinTrack.Application.Transactions.DTOs;
using FinTrack.Application.Transactions.Interfaces;
using FinTrack.Domain.Enums;
using FinTrack.Web.ViewModels.Transactions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinTrack.Web.Controllers;

[Authorize]
public class TransactionsController : Controller
{
    private readonly ITransactionService _transactionService;
    private readonly IAccountService _accountService;
    private readonly ICategoryService _categoryService;

    public TransactionsController(
        ITransactionService transactionService,
        IAccountService accountService,
        ICategoryService categoryService)
    {
        _transactionService = transactionService;
        _accountService = accountService;
        _categoryService = categoryService;
    }

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var transactions = await _transactionService.GetAllAsync(
            CurrentUserId,
            cancellationToken);

        return View(transactions);
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        CancellationToken cancellationToken)
    {
        var model = new CreateTransactionViewModel();

        await PopulateDropdownsAsync(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateTransactionViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var request = new CreateTransactionRequest(
                model.AccountId,
                model.CategoryId,
                model.Amount,
                model.Type,
                model.TransactionDate,
                model.Note);

            await _transactionService.CreateAsync(
                CurrentUserId,
                request,
                cancellationToken);

            TempData["SuccessMessage"] =
                "Transaction created successfully.";

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

        await PopulateDropdownsAsync(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        var deleted = await _transactionService.SoftDeleteAsync(
            CurrentUserId,
            id,
            cancellationToken);

        if (!deleted)
            return NotFound();

        TempData["SuccessMessage"] =
            "Transaction deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync(
        CreateTransactionViewModel model,
        CancellationToken cancellationToken)
    {
        var accounts = await _accountService.GetAllAsync(
            CurrentUserId,
            cancellationToken);

        var categories = await _categoryService.GetAllAsync(
            CurrentUserId,
            cancellationToken);

        model.Accounts = accounts
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(CultureInfo.InvariantCulture),
                Text = $"{a.Name} ({a.Type})"
            })
            .ToList();

        model.Categories = categories
            .Where(c =>
                c.Type == (model.Type == TransactionType.Income
                    ? CategoryType.Income
                    : CategoryType.Expense))
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(CultureInfo.InvariantCulture),
                Text = c.Name
            })
            .ToList();
    }

    [HttpGet]
    public async Task<IActionResult> GetCategoriesByType(
        TransactionType type,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(type))
            return BadRequest();

        var categories = await _categoryService.GetAllAsync(
            CurrentUserId,
            cancellationToken);

        var categoryType = type == TransactionType.Income
            ? CategoryType.Income
            : CategoryType.Expense;

        var result = categories
            .Where(c => c.Type == categoryType)
            .Select(c => new
            {
                id = c.Id,
                name = c.Name
            });

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        long id,
        CancellationToken cancellationToken)
    {
        var transaction = await _transactionService.GetByIdAsync(
            CurrentUserId,
            id,
            cancellationToken);

        if (transaction is null)
            return NotFound();

        var model = new EditTransactionViewModel
        {
            Id = transaction.Id,
            CategoryId = transaction.CategoryId,
            Amount = transaction.Amount,
            TransactionDate = transaction.TransactionDate,
            Note = transaction.Note,
            AccountName = transaction.AccountName,
            Type = transaction.Type
        };

        await PopulateEditCategoriesAsync(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        long id,
        EditTransactionViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Id)
            return BadRequest();

        var existing = await _transactionService.GetByIdAsync(
            CurrentUserId,
            id,
            cancellationToken);

        if (existing is null)
            return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var request = new UpdateTransactionRequest(
                    model.CategoryId,
                    model.Amount,
                    model.TransactionDate,
                    model.Note);

                var updated = await _transactionService.UpdateAsync(
                    CurrentUserId,
                    id,
                    request,
                    cancellationToken);

                if (!updated)
                    return NotFound();

                TempData["SuccessMessage"] =
                    "Transaction updated successfully.";

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

        model.AccountName = existing.AccountName;
        model.Type = existing.Type;

        await PopulateEditCategoriesAsync(model, cancellationToken);

        return View(model);
    }


    private async Task PopulateEditCategoriesAsync(
        EditTransactionViewModel model,
        CancellationToken cancellationToken)
    {
        var categories = await _categoryService.GetAllAsync(
            CurrentUserId,
            cancellationToken);

        var expectedType = model.Type == TransactionType.Income
            ? CategoryType.Income
            : CategoryType.Expense;

        model.Categories = categories
            .Where(c => c.Type == expectedType)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToList();
    }


}
