
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Transactions.DTOs;

public sealed record CreateTransactionRequest(
    int AccountId,
    int CategoryId,
    decimal Amount,
    TransactionType Type,
    DateTime TransactionDate,
    string? Note
);
