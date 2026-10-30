
namespace FinTrack.Application.Transactions.DTOs;

public sealed record UpdateTransactionRequest(
    int CategoryId,
    decimal Amount,
    DateTime TransactionDate,
    string? Note
);
