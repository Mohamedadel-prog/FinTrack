
using FinTrack.Application.Common;
using FinTrack.Application.Transfers.DTOs;

namespace FinTrack.Application.Transfers.Interfaces;

public interface ITransferService
{
    Task<long> CreateAsync(
        string userId,
        CreateTransferRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TransferDto>> GetPagedAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        string userId,
        long transferId,
        CancellationToken cancellationToken = default);
}
