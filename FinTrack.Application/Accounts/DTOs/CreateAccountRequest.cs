
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Accounts.DTOs;

public sealed record CreateAccountRequest(
    string Name,
    AccountType Type,
    decimal OpeningBalance,
    string Currency
);
