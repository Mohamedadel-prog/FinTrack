
namespace FinTrack.Application.Transfers.DTOs;

public sealed record CreateTransferRequest(
    int FromAccountId,
    int ToAccountId,
    decimal Amount,
    DateTime TransferDate,
    string? Note
);
