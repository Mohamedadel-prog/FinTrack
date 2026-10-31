
using FinTrack.Application.Accounts.DTOs;
using FinTrack.Application.Accounts.Interfaces;
using FinTrack.Domain.Entities;
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
            request.OpeningBalance,
            request.Currency);

        _context.Accounts.Add(account);

        await _context.SaveChangesAsync(cancellationToken);

        return account.Id;
    }

    public Task<IReadOnlyList<AccountDto>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        GetListAsync(userId, archived: false, cancellationToken);

    public Task<IReadOnlyList<AccountDto>> GetArchivedAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        GetListAsync(userId, archived: true, cancellationToken);

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

        var movements = await AccountBalances.GetNetMovementsAsync(
            _context, userId, [account.Id], cancellationToken);

        return ToDto(account, movements);
    }

    public async Task<bool> UpdateAsync(
        string userId,
        int accountId,
        UpdateAccountRequest request,
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

        var currency = request.Currency?.Trim().ToUpperInvariant();

        if (currency != account.Currency)
        {
            // Changing the currency would silently reinterpret every amount
            // already recorded against this account.
            var hasActivity =
                await _context.Transactions.AnyAsync(
                    t => t.AccountId == accountId && !t.IsDeleted,
                    cancellationToken) ||
                await _context.Transfers.AnyAsync(
                    t => (t.FromAccountId == accountId ||
                          t.ToAccountId == accountId) &&
                         !t.IsDeleted,
                    cancellationToken);

            if (hasActivity)
                throw new InvalidOperationException(
                    "The currency can't be changed once the account has transactions or transfers.");

            account.ChangeCurrency(request.Currency!);
        }

        account.Rename(request.Name);
        account.ChangeType(request.Type);

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

    public async Task<bool> UnarchiveAsync(
        string userId,
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
            .FirstOrDefaultAsync(
                a => a.Id == accountId &&
                     a.UserId == userId &&
                     a.IsArchived,
                cancellationToken);

        if (account is null)
            return false;

        account.Unarchive();

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<IReadOnlyList<AccountDto>> GetListAsync(
        string userId,
        bool archived,
        CancellationToken cancellationToken)
    {
        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a =>
                a.UserId == userId &&
                a.IsArchived == archived)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

        if (accounts.Count == 0)
            return [];

        var movements = await AccountBalances.GetNetMovementsAsync(
            _context,
            userId,
            accounts.Select(a => a.Id).ToArray(),
            cancellationToken);

        return accounts
            .Select(account => ToDto(account, movements))
            .ToList();
    }

    private static AccountDto ToDto(
        Account account,
        IReadOnlyDictionary<int, decimal> movements) =>
        new(
            account.Id,
            account.Name,
            account.Type,
            account.OpeningBalance,
            account.Currency,
            account.OpeningBalance + movements.GetValueOrDefault(account.Id));
}
