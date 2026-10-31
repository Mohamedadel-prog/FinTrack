
using FinTrack.Application.Dashboard.DTOs;
using FinTrack.Application.Dashboard.Interfaces;
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;
using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> GetDashboardAsync(
        string userId,
        DateTime today,
        string? currency = null,
        CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var chartStart = monthStart.AddMonths(-5);

        // Amounts in different currencies can't be summed, so the dashboard
        // shows one currency at a time: the requested one, or the currency
        // most of the user's active accounts use.
        var currencies = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId && !a.IsArchived)
            .GroupBy(a => a.Currency)
            .Select(g => new { Currency = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Currency)
            .Select(x => x.Currency)
            .ToListAsync(cancellationToken);

        var selectedCurrency =
            currencies.FirstOrDefault(c =>
                string.Equals(c, currency, StringComparison.OrdinalIgnoreCase))
            ?? currencies.FirstOrDefault()
            ?? Currencies.Default;

        // Active accounts and opening balances
        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a =>
                a.UserId == userId &&
                !a.IsArchived &&
                a.Currency == selectedCurrency)
            .OrderBy(a => a.Name)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.OpeningBalance
            })
            .ToListAsync(cancellationToken);

        var activeAccountIds = accounts
            .Select(a => a.Id)
            .ToArray();

        // Non-deleted transactions that belong to the user's active accounts.
        // Every figure below is built on this, so archived accounts are
        // excluded consistently from balance, monthly totals and charts.
        var activeTransactions = _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                !t.IsDeleted &&
                activeAccountIds.Contains(t.AccountId));

        // Net movement per account (transactions and transfers)
        // -> current balance per account
        var netByAccount = await AccountBalances.GetNetMovementsAsync(
            _context,
            userId,
            activeAccountIds,
            cancellationToken);

        var accountBalances = accounts
            .Select(a => new AccountBalanceDto(
                a.Id,
                a.Name,
                a.OpeningBalance + netByAccount.GetValueOrDefault(a.Id)))
            .ToList();

        var totalBalance = accountBalances.Sum(a => a.Balance);

        // Current month income and expenses
        var monthlyTotals = await activeTransactions
            .Where(t =>
                t.TransactionDate >= monthStart &&
                t.TransactionDate < nextMonth)
            .GroupBy(t => t.Type)
            .Select(g => new
            {
                Type = g.Key,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        var monthlyIncome = monthlyTotals
            .Where(x => x.Type == TransactionType.Income)
            .Sum(x => x.Total);

        var monthlyExpenses = monthlyTotals
            .Where(x => x.Type == TransactionType.Expense)
            .Sum(x => x.Total);

        // Last six months
        var chartRows = await activeTransactions
            .Where(t =>
                t.TransactionDate >= chartStart &&
                t.TransactionDate < nextMonth)
            .GroupBy(t => new
            {
                t.TransactionDate.Year,
                t.TransactionDate.Month,
                t.Type
            })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                g.Key.Type,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        var monthlyChart = Enumerable.Range(0, 6)
            .Select(i =>
            {
                var month = chartStart.AddMonths(i);

                return new MonthlyFinanceDto(
                    month.ToString("MMM yyyy"),
                    chartRows
                        .Where(x => x.Year == month.Year &&
                                    x.Month == month.Month &&
                                    x.Type == TransactionType.Income)
                        .Sum(x => x.Total),
                    chartRows
                        .Where(x => x.Year == month.Year &&
                                    x.Month == month.Month &&
                                    x.Type == TransactionType.Expense)
                        .Sum(x => x.Total)
                );
            })
            .ToList();

        // Expenses by category for the current month
        var expensesByCategory = await (
            from t in activeTransactions
            join c in _context.Categories.AsNoTracking()
                on t.CategoryId equals c.Id
            where c.UserId == userId
                  && t.Type == TransactionType.Expense
                  && t.TransactionDate >= monthStart
                  && t.TransactionDate < nextMonth
            group t by c.Name into g
            orderby g.Sum(t => t.Amount) descending
            select new CategoryExpenseDto(
                g.Key,
                g.Sum(t => t.Amount))
        ).ToListAsync(cancellationToken);

        // Last five transactions
        var recentTransactions = await (
            from t in activeTransactions
            join a in _context.Accounts.AsNoTracking()
                on t.AccountId equals a.Id
            join c in _context.Categories.AsNoTracking()
                on t.CategoryId equals c.Id
            where a.UserId == userId
                  && c.UserId == userId
            orderby t.TransactionDate descending, t.Id descending
            select new RecentTransactionDto(
                t.Id,
                c.Name,
                a.Name,
                t.Amount,
                t.Type == TransactionType.Income,
                t.TransactionDate)
        )
        .Take(5)
        .ToListAsync(cancellationToken);

        return new DashboardDto(
            totalBalance,
            monthlyIncome,
            monthlyExpenses,
            monthlyIncome - monthlyExpenses,
            accounts.Count,
            accountBalances,
            monthlyChart,
            expensesByCategory,
            recentTransactions,
            selectedCurrency,
            currencies
        );
    }
}
