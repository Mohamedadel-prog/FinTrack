
namespace FinTrack.Application.Transfers.DTOs;

public sealed record TransferDto(
    long Id,
    int FromAccountId,
    string FromAccountName,
    int ToAccountId,
    string ToAccountName,
    decimal Amount,
    string Currency,
    DateTime TransferDate,
    string? Note
);
