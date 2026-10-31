
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Transactions.DTOs;

public sealed record TransactionDto(
    long Id,
    int AccountId,
    string AccountName,
    int CategoryId,
    string CategoryName,
    decimal Amount,
    TransactionType Type,
    DateTime TransactionDate,
    string? Note,
    string Currency,
    bool CategoryIsArchived
);
