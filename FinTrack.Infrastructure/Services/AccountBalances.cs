
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Services;

/// <summary>
/// Net movement per account: income minus expenses, plus transfers in,
/// minus transfers out. Opening balance + net movement = current balance.
/// </summary>
internal static class AccountBalances
{
    public static async Task<Dictionary<int, decimal>> GetNetMovementsAsync(
        ApplicationDbContext context,
        string userId,
        int[] accountIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, decimal>();

        if (accountIds.Length == 0)
            return result;

        var transactionNet = await context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                !t.IsDeleted &&
                accountIds.Contains(t.AccountId))
            .GroupBy(t => t.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Amount = g.Sum(t =>
                    t.Type == TransactionType.Income
                        ? t.Amount
                        : -t.Amount)
            })
            .ToListAsync(cancellationToken);

        var transfersOut = await context.Transfers
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                !t.IsDeleted &&
                accountIds.Contains(t.FromAccountId))
            .GroupBy(t => t.FromAccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Amount = -g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        var transfersIn = await context.Transfers
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                !t.IsDeleted &&
                accountIds.Contains(t.ToAccountId))
            .GroupBy(t => t.ToAccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        foreach (var row in transactionNet.Concat(transfersOut).Concat(transfersIn))
        {
            result[row.AccountId] =
                result.GetValueOrDefault(row.AccountId) + row.Amount;
        }

        return result;
    }
}
