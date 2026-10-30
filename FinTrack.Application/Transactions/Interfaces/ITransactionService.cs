
using FinTrack.Application.Transactions.DTOs;

namespace FinTrack.Application.Transactions.Interfaces;

public interface ITransactionService
{
    Task<long> CreateAsync(
        string userId,
        CreateTransactionRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransactionDto>> GetAllAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        string userId,
        long transactionId,
        CancellationToken cancellationToken = default);

    Task<TransactionDto?> GetByIdAsync(
        string userId,
        long transactionId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        string userId,
        long transactionId,
        UpdateTransactionRequest request,
        CancellationToken cancellationToken = default);

}
