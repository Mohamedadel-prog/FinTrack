
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Accounts.DTOs;

public sealed record UpdateAccountRequest(
    string Name,
    AccountType Type,
    string Currency
);
