
using FinTrack.Application.Transactions.DTOs;
using FinTrack.Application.Transactions.Interfaces;
using FinTrack.Domain.Entities;
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

    public async Task<IReadOnlyList<TransactionDto>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from transaction in _context.Transactions.AsNoTracking()
            join account in _context.Accounts.AsNoTracking()
                on transaction.AccountId equals account.Id
            join category in _context.Categories.AsNoTracking()
                on transaction.CategoryId equals category.Id
            where transaction.UserId == userId
                  && account.UserId == userId
                  && category.UserId == userId
                  && !transaction.IsDeleted
            orderby transaction.TransactionDate descending,
                    transaction.Id descending
            select new TransactionDto(
                transaction.Id,
                transaction.AccountId,
                account.Name,
                transaction.CategoryId,
                category.Name,
                transaction.Amount,
                transaction.Type,
                transaction.TransactionDate,
                transaction.Note)
        ).ToListAsync(cancellationToken);
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
                !t.IsDeleted,
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
        return await (
            from transaction in _context.Transactions.AsNoTracking()
            join account in _context.Accounts.AsNoTracking()
                on transaction.AccountId equals account.Id
            join category in _context.Categories.AsNoTracking()
                on transaction.CategoryId equals category.Id
            where transaction.Id == transactionId
                  && transaction.UserId == userId
                  && account.UserId == userId
                  && category.UserId == userId
                  && !transaction.IsDeleted
            select new TransactionDto(
                transaction.Id,
                transaction.AccountId,
                account.Name,
                transaction.CategoryId,
                category.Name,
                transaction.Amount,
                transaction.Type,
                transaction.TransactionDate,
                transaction.Note)
        ).FirstOrDefaultAsync(cancellationToken);
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
            throw new InvalidOperationException(
                "The transaction account is no longer available.");

        
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

}
