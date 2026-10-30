
using FinTrack.Application.Dashboard.DTOs;
using FinTrack.Application.Dashboard.Interfaces;
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
        CancellationToken cancellationToken = default)
    {
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var chartStart = monthStart.AddMonths(-5);

        // Active accounts and opening balances
        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId && !a.IsArchived)
            .Select(a => new
            {
                a.Id,
                a.OpeningBalance
            })
            .ToListAsync(cancellationToken);

        var activeAccountIds = accounts
            .Select(a => a.Id)
            .ToArray();

        var openingTotal = accounts.Sum(a => a.OpeningBalance);

        // All active transactions for current user's active accounts
        var movements = await _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                !t.IsDeleted &&
                activeAccountIds.Contains(t.AccountId))
            .GroupBy(t => t.Type)
            .Select(g => new
            {
                Type = g.Key,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        var totalIncome = movements
            .Where(x => x.Type == TransactionType.Income)
            .Sum(x => x.Total);

        var totalExpenses = movements
            .Where(x => x.Type == TransactionType.Expense)
            .Sum(x => x.Total);

        var totalBalance = openingTotal + totalIncome - totalExpenses;

        // Current month income and expenses
        var monthlyTotals = await _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                !t.IsDeleted &&
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
        var chartRows = await _context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                !t.IsDeleted &&
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
            from t in _context.Transactions.AsNoTracking()
            join c in _context.Categories.AsNoTracking()
                on t.CategoryId equals c.Id
            where t.UserId == userId
                  && c.UserId == userId
                  && !t.IsDeleted
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
            from t in _context.Transactions.AsNoTracking()
            join a in _context.Accounts.AsNoTracking()
                on t.AccountId equals a.Id
            join c in _context.Categories.AsNoTracking()
                on t.CategoryId equals c.Id
            where t.UserId == userId
                  && a.UserId == userId
                  && c.UserId == userId
                  && !t.IsDeleted
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
            monthlyChart,
            expensesByCategory,
            recentTransactions
        );
    }
}
