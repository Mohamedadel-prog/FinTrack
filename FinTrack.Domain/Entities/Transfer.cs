
namespace FinTrack.Domain.Entities;

public class Transfer
{
    public const int MaxNoteLength = 500;

    public long Id { get; private set; }

    public string UserId { get; private set; } = null!;

    public int FromAccountId { get; private set; }

    public int ToAccountId { get; private set; }

    public decimal Amount { get; private set; }

    public DateTime TransferDate { get; private set; }

    public string? Note { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    private Transfer() { }

    public Transfer(
        string userId,
        int fromAccountId,
        int toAccountId,
        decimal amount,
        DateTime transferDate,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(
                "User ID is required.", nameof(userId));

        if (fromAccountId <= 0)
            throw new ArgumentOutOfRangeException(nameof(fromAccountId));

        if (toAccountId <= 0)
            throw new ArgumentOutOfRangeException(nameof(toAccountId));

        if (fromAccountId == toAccountId)
            throw new ArgumentException(
                "Choose two different accounts.", nameof(toAccountId));

        if (amount <= 0 || decimal.Round(amount, 2) != amount)
            throw new ArgumentException(
                "Amount must be positive with at most 2 decimal places.",
                nameof(amount));

        if (transferDate == default)
            throw new ArgumentException(
                "Transfer date is required.", nameof(transferDate));

        if (note?.Length > MaxNoteLength)
            throw new ArgumentException(
                $"Note cannot exceed {MaxNoteLength} characters.",
                nameof(note));

        UserId = userId;
        FromAccountId = fromAccountId;
        ToAccountId = toAccountId;
        Amount = amount;
        TransferDate = transferDate;
        Note = string.IsNullOrWhiteSpace(note)
            ? null
            : note.Trim();
        IsDeleted = false;
    }

    public void SoftDelete(DateTime deletedAtUtc)
    {
        if (IsDeleted)
            throw new InvalidOperationException(
                "Transfer is already deleted.");

        if (deletedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "Deletion timestamp must be UTC.",
                nameof(deletedAtUtc));

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
    }
}
