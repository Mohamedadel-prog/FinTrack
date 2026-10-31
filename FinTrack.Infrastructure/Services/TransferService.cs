
using FinTrack.Application.Common;
using FinTrack.Application.Transfers.DTOs;
using FinTrack.Application.Transfers.Interfaces;
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Infrastructure.Services;

public sealed class TransferService : ITransferService
{
    private readonly ApplicationDbContext _context;

    public TransferService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<long> CreateAsync(
        string userId,
        CreateTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FromAccountId == request.ToAccountId)
            throw new ArgumentException(
                "Choose two different accounts.");

        var accounts = await _context.Accounts
            .AsNoTracking()
            .Where(a =>
                (a.Id == request.FromAccountId ||
                 a.Id == request.ToAccountId) &&
                a.UserId == userId &&
                !a.IsArchived)
            .Select(a => new { a.Id, a.Currency })
            .ToListAsync(cancellationToken);

        var source = accounts.FirstOrDefault(a => a.Id == request.FromAccountId);
        var destination = accounts.FirstOrDefault(a => a.Id == request.ToAccountId);

        if (source is null || destination is null)
            throw new InvalidOperationException(
                "Account not found or unavailable.");

        if (source.Currency != destination.Currency)
            throw new InvalidOperationException(
                "Transfers are only supported between accounts in the same currency.");

        var transfer = new Transfer(
            userId,
            request.FromAccountId,
            request.ToAccountId,
            request.Amount,
            request.TransferDate,
            request.Note);

        _context.Transfers.Add(transfer);

        await _context.SaveChangesAsync(cancellationToken);

        return transfer.Id;
    }

    public async Task<PagedResult<TransferDto>> GetPagedAsync(
        string userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = PagedResult<TransferDto>.NormalizePage(page);
        pageSize = PagedResult<TransferDto>.NormalizePageSize(pageSize);

        // Only transfers whose both sides are still active, matching how
        // transactions of archived accounts are hidden.
        var query =
            from transfer in _context.Transfers.AsNoTracking()
            join fromAccount in _context.Accounts.AsNoTracking()
                on transfer.FromAccountId equals fromAccount.Id
            join toAccount in _context.Accounts.AsNoTracking()
                on transfer.ToAccountId equals toAccount.Id
            where transfer.UserId == userId
                  && !transfer.IsDeleted
                  && fromAccount.UserId == userId
                  && !fromAccount.IsArchived
                  && toAccount.UserId == userId
                  && !toAccount.IsArchived
            select new { transfer, fromAccount, toAccount };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.transfer.TransferDate)
            .ThenByDescending(x => x.transfer.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TransferDto(
                x.transfer.Id,
                x.transfer.FromAccountId,
                x.fromAccount.Name,
                x.transfer.ToAccountId,
                x.toAccount.Name,
                x.transfer.Amount,
                x.fromAccount.Currency,
                x.transfer.TransferDate,
                x.transfer.Note))
            .ToListAsync(cancellationToken);

        return new PagedResult<TransferDto>(
            items, page, pageSize, totalCount);
    }

    public async Task<bool> SoftDeleteAsync(
        string userId,
        long transferId,
        CancellationToken cancellationToken = default)
    {
        var transfer = await _context.Transfers
            .FirstOrDefaultAsync(t =>
                t.Id == transferId &&
                t.UserId == userId &&
                !t.IsDeleted &&
                _context.Accounts.Any(a =>
                    a.Id == t.FromAccountId &&
                    a.UserId == userId &&
                    !a.IsArchived) &&
                _context.Accounts.Any(a =>
                    a.Id == t.ToAccountId &&
                    a.UserId == userId &&
                    !a.IsArchived),
                cancellationToken);

        if (transfer is null)
            return false;

        transfer.SoftDelete(DateTime.UtcNow);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
