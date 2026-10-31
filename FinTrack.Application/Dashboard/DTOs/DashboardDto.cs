
namespace FinTrack.Application.Dashboard.DTOs;

public sealed record DashboardDto(
    decimal TotalBalance,
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal NetCashFlow,
    int ActiveAccounts,
    IReadOnlyList<AccountBalanceDto> Accounts,
    IReadOnlyList<MonthlyFinanceDto> MonthlyChart,
    IReadOnlyList<CategoryExpenseDto> ExpensesByCategory,
    IReadOnlyList<RecentTransactionDto> RecentTransactions,
    string Currency,
    IReadOnlyList<string> Currencies
);

public sealed record AccountBalanceDto(
    int Id,
    string Name,
    decimal Balance
);

public sealed record MonthlyFinanceDto(
    string Month,
    decimal Income,
    decimal Expenses
);

public sealed record CategoryExpenseDto(
    string Category,
    decimal Amount
);

public sealed record RecentTransactionDto(
    long Id,
    string Category,
    string Account,
    decimal Amount,
    bool IsIncome,
    DateTime Date
);
