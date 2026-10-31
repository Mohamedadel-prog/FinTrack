using FinTrack.Application.Accounts.DTOs;

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

    Task<IReadOnlyList<AccountDto>> GetArchivedAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<AccountDto?> GetByIdAsync(
        string userId,
        int accountId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        string userId,
        int accountId,
        UpdateAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(
        string userId,
        int accountId,
        CancellationToken cancellationToken = default);

    Task<bool> UnarchiveAsync(
        string userId,
        int accountId,
        CancellationToken cancellationToken = default);
}
