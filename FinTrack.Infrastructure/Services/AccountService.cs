
using FinTrack.Application.Accounts.DTOs;
using FinTrack.Application.Accounts.Interfaces;
using FinTrack.Domain.Entities;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Services;

public sealed class AccountService : IAccountService
{
    private readonly ApplicationDbContext _context;

    public AccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateAsync(
        string userId,
        CreateAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var account = new Account(
            userId,
            request.Name,
            request.Type,
            request.OpeningBalance);

        _context.Accounts.Add(account);

        await _context.SaveChangesAsync(cancellationToken);

        return account.Id;
    }


    public async Task<IReadOnlyList<AccountDto>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a =>
                a.UserId == userId &&
                !a.IsArchived)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
            return [];

        var accountIds = accounts.Select(a => a.Id).ToArray();

        var movements = await _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                !t.IsDeleted &&
                accountIds.Contains(t.AccountId))
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                NetAmount = g.Sum(t =>
                    t.Type == TransactionType.Income
                        ? t.Amount
                        : -t.Amount)
            })
            .ToDictionaryAsync(
                x => x.AccountId,
                x => x.NetAmount,
                cancellationToken);

        return accounts.Select(account =>
        {
            movements.TryGetValue(
                account.Id,
                out var netAmount);

            return new AccountDto(
                account.Id,
                account.Name,
                account.Type,
                account.OpeningBalance,
                account.Currency,
                account.OpeningBalance + netAmount);
        }).ToList();
    }



    public async Task<AccountDto?> GetByIdAsync(
        string userId,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a =>
                a.Id == accountId &&
                a.UserId == userId &&
                !a.IsArchived,
                cancellationToken);

        if (account is null)
            return null;

        var netAmount = await _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.AccountId == accountId &&
                t.UserId == userId &&
                !t.IsDeleted)
            .SumAsync(t =>
                t.Type == TransactionType.Income
                    ? t.Amount
                    : -t.Amount,
                cancellationToken);

        return new AccountDto(
            account.Id,
            account.Name,
            account.Type,
            account.OpeningBalance,
            account.Currency,
            account.OpeningBalance + netAmount);
    }


    public async Task<bool> UpdateAsync(
        string userId,
        int accountId,
        string name,
        AccountType type,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(
                a => a.Id == accountId &&
                     a.UserId == userId &&
                     !a.IsArchived,
                cancellationToken);

        if (account is null)
            return false;

        account.Rename(name);
        account.ChangeType(type);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> ArchiveAsync(
        string userId,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(
                a => a.Id == accountId &&
                     a.UserId == userId &&
                     !a.IsArchived,
                cancellationToken);

        if (account is null)
            return false;

        account.Archive();

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

}
