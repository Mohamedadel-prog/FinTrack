
using FinTrack.Application.Common;
using FinTrack.Application.Transactions.DTOs;
using FinTrack.Application.Transactions.Interfaces;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

using FinancialTransaction = FinTrack.Domain.Entities.Transaction;

namespace FinTrack.Infrastructure.Services;

public sealed class TransactionService : ITransactionService
{
    private readonly ApplicationDbContext _context;

    public TransactionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<long> CreateAsync(
        string userId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {

        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.Id == request.AccountId &&
                a.UserId == userId &&
                !a.IsArchived,
                cancellationToken);

        if (account is null)
            throw new InvalidOperationException(
                "Account not found or unavailable.");


        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId &&
                c.UserId == userId &&
                !c.IsArchived,
                cancellationToken);

        if (category is null)
            throw new InvalidOperationException(
                "Category not found or unavailable.");


        var expectedCategoryType = request.Type switch
        {
            TransactionType.Income => CategoryType.Income,
            TransactionType.Expense => CategoryType.Expense,
            _ => throw new ArgumentException(
                "Invalid transaction type.")
        };

        if (category.Type != expectedCategoryType)
            throw new InvalidOperationException(
                "Category type does not match transaction type.");


        var transaction = new FinancialTransaction(
            userId,
            request.AccountId,
            request.CategoryId,
            request.Amount,
            request.Type,
            request.TransactionDate,
            request.Note);


        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }

    public async Task<TransactionListResult> GetPagedAsync(
        string userId,
        TransactionFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = PagedResult<TransactionDto>.NormalizePage(filter.Page);
        var pageSize = PagedResult<TransactionDto>.NormalizePageSize(filter.PageSize);

        var query = VisibleTransactions(userId);

        if (filter.From is { } from)
            query = query.Where(x => x.Transaction.TransactionDate >= from.Date);

        if (filter.To is { } to)
        {
            var dayAfter = to.Date.AddDays(1);
            query = query.Where(x => x.Transaction.TransactionDate < dayAfter);
        }

        if (filter.AccountId is { } accountId)
            query = query.Where(x => x.Transaction.AccountId == accountId);

        if (filter.CategoryId is { } categoryId)
            query = query.Where(x => x.Transaction.CategoryId == categoryId);

        if (filter.Type is { } type)
            query = query.Where(x => x.Transaction.Type == type);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();

            query = query.Where(x =>
                x.Category.Name.Contains(term) ||
                x.Account.Name.Contains(term) ||
                (x.Transaction.Note != null &&
                 x.Transaction.Note.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Totals cover every matching row, not just the current page,
        // and are kept per currency because they can't be added together.
        var totals = await query
            .GroupBy(x => x.Account.Currency)
            .Select(g => new CurrencyTotalsDto(
                g.Key,
                g.Sum(x => x.Transaction.Type == TransactionType.Income
                    ? x.Transaction.Amount
                    : 0m),
                g.Sum(x => x.Transaction.Type == TransactionType.Expense
                    ? x.Transaction.Amount
                    : 0m)))
            .ToListAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Transaction.TransactionDate)
            .ThenByDescending(x => x.Transaction.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TransactionDto(
                x.Transaction.Id,
                x.Transaction.AccountId,
                x.Account.Name,
                x.Transaction.CategoryId,
                x.Category.Name,
                x.Transaction.Amount,
                x.Transaction.Type,
                x.Transaction.TransactionDate,
                x.Transaction.Note,
                x.Account.Currency,
                x.Category.IsArchived))
            .ToListAsync(cancellationToken);

        return new TransactionListResult(
            new PagedResult<TransactionDto>(items, page, pageSize, totalCount),
            totals
                .OrderBy(t => t.Currency)
                .ToList());
    }

    public async Task<bool> SoftDeleteAsync(
        string userId,
        long transactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == transactionId &&
                t.UserId == userId &&
                !t.IsDeleted &&
                _context.Accounts.Any(a =>
                    a.Id == t.AccountId &&
                    a.UserId == userId &&
                    !a.IsArchived),
                cancellationToken);

        if (transaction is null)
            return false;

        transaction.SoftDelete(DateTime.UtcNow);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<TransactionDto?> GetByIdAsync(
        string userId,
        long transactionId,
        CancellationToken cancellationToken = default)
    {
        return await VisibleTransactions(userId)
            .Where(x => x.Transaction.Id == transactionId)
            .Select(x => new TransactionDto(
                x.Transaction.Id,
                x.Transaction.AccountId,
                x.Account.Name,
                x.Transaction.CategoryId,
                x.Category.Name,
                x.Transaction.Amount,
                x.Transaction.Type,
                x.Transaction.TransactionDate,
                x.Transaction.Note,
                x.Account.Currency,
                x.Category.IsArchived))
            .FirstOrDefaultAsync(cancellationToken);
    }


    public async Task<bool> UpdateAsync(
        string userId,
        long transactionId,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t =>
                t.Id == transactionId &&
                t.UserId == userId &&
                !t.IsDeleted,
                cancellationToken);

        if (transaction is null)
            return false;


        var accountExists = await _context.Accounts
            .AsNoTracking()
            .AnyAsync(a =>
                a.Id == transaction.AccountId &&
                a.UserId == userId &&
                !a.IsArchived,
                cancellationToken);

        if (!accountExists)
            return false;

        // An archived category can't be picked for new entries, but a
        // transaction that already uses one may keep it when edited.
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == request.CategoryId &&
                c.UserId == userId &&
                (!c.IsArchived || c.Id == transaction.CategoryId),
                cancellationToken);

        if (category is null)
            throw new InvalidOperationException(
                "Category not found or unavailable.");


        var expectedType = transaction.Type switch
        {
            TransactionType.Income => CategoryType.Income,
            TransactionType.Expense => CategoryType.Expense,
            _ => throw new InvalidOperationException(
                "Invalid transaction type.")
        };

        if (category.Type != expectedType)
            throw new InvalidOperationException(
                "Category type does not match transaction type.");


        transaction.Update(
            request.CategoryId,
            request.Amount,
            request.TransactionDate,
            request.Note);


        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // Non-deleted transactions of the user's active accounts, with their
    // account and category (archived categories included).
    private IQueryable<TransactionRow> VisibleTransactions(string userId) =>
        from transaction in _context.Transactions.AsNoTracking()
        join account in _context.Accounts.AsNoTracking()
            on transaction.AccountId equals account.Id
        join category in _context.Categories.AsNoTracking()
            on transaction.CategoryId equals category.Id
        where transaction.UserId == userId
              && account.UserId == userId
              && !account.IsArchived
              && category.UserId == userId
              && !transaction.IsDeleted
        select new TransactionRow
        {
            Transaction = transaction,
            Account = account,
            Category = category
        };

    private sealed class TransactionRow
    {
        public FinancialTransaction Transaction { get; init; } = null!;

        public Domain.Entities.Account Account { get; init; } = null!;

        public Domain.Entities.Category Category { get; init; } = null!;
    }
}
