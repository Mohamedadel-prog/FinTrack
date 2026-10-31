
using FinTrack.Application.Common;
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Transactions.DTOs;

/// <summary>
/// Optional filters for the transaction list. Null means "don't filter".
/// <see cref="To"/> is inclusive of the whole day.
/// </summary>
public sealed record TransactionFilter(
    DateTime? From = null,
    DateTime? To = null,
    int? AccountId = null,
    int? CategoryId = null,
    TransactionType? Type = null,
    string? Search = null,
    int Page = 1,
    int PageSize = PagedResult<TransactionDto>.DefaultPageSize
);

public sealed record CurrencyTotalsDto(
    string Currency,
    decimal Income,
    decimal Expenses
);

public sealed record TransactionListResult(
    PagedResult<TransactionDto> Transactions,
    IReadOnlyList<CurrencyTotalsDto> Totals
);
