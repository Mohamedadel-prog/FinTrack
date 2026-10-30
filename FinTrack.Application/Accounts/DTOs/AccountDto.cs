
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Accounts.DTOs;

public sealed record AccountDto(
    int Id,
    string Name,
    AccountType Type,
    decimal OpeningBalance,
    string Currency,
    decimal CurrentBalance
);
