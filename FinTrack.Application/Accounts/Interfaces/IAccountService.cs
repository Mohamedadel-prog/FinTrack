using FinTrack.Application.Accounts.DTOs;
using FinTrack.Domain.Enums;

namespace FinTrack.Application.Accounts.Interfaces;

public interface IAccountService
{
    Task<int> CreateAsync(
        string userId,
        CreateAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountDto>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<AccountDto?> GetByIdAsync(
        string userId,
        int accountId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        string userId,
        int accountId,
        string name,
        AccountType type,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(
        string userId,
        int accountId,
        CancellationToken cancellationToken = default);
}
