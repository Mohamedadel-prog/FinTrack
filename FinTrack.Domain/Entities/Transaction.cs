
using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class Transaction
{
    public long Id { get; private set; }

    public string UserId { get; private set; } = null!;

    public int AccountId { get; private set; }

    public int CategoryId { get; private set; }

    public decimal Amount { get; private set; }

    public TransactionType Type { get; private set; }

    public DateTime TransactionDate { get; private set; }

    public string? Note { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    private Transaction() { }

    public Transaction(
        string userId,
        int accountId,
        int categoryId,
        decimal amount,
        TransactionType type,
        DateTime transactionDate,
        string? note = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(
                "User ID is required.", nameof(userId));

        if (accountId <= 0)
            throw new ArgumentOutOfRangeException(nameof(accountId));

        if (categoryId <= 0)
            throw new ArgumentOutOfRangeException(nameof(categoryId));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount), "Amount must be positive.");

        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException(
                "Amount cannot have more than 2 decimal places.",
                nameof(amount));

        if (!Enum.IsDefined(type))
            throw new ArgumentException(
                "Invalid transaction type.", nameof(type));

        if (transactionDate == default)
            throw new ArgumentException(
                "Transaction date is required.",
                nameof(transactionDate));

        if (note?.Length > 500)
            throw new ArgumentException(
                "Note cannot exceed 500 characters.",
                nameof(note));

        UserId = userId;
        AccountId = accountId;
        CategoryId = categoryId;
        Amount = amount;
        Type = type;
        TransactionDate = transactionDate;
        Note = string.IsNullOrWhiteSpace(note)
            ? null
            : note.Trim();
        IsDeleted = false;
    }

    public void Update(
        int categoryId,
        decimal amount,
        DateTime transactionDate,
        string? note)
    {
        if (IsDeleted)
            throw new InvalidOperationException(
                "Deleted transactions cannot be modified.");

        if (categoryId <= 0)
            throw new ArgumentOutOfRangeException(nameof(categoryId));

        if (amount <= 0 || decimal.Round(amount, 2) != amount)
            throw new ArgumentException(
                "Amount must be positive with at most 2 decimal places.",
                nameof(amount));

        if (transactionDate == default)
            throw new ArgumentException(
                "Transaction date is required.",
                nameof(transactionDate));

        if (note?.Length > 500)
            throw new ArgumentException(
                "Note cannot exceed 500 characters.",
                nameof(note));

        CategoryId = categoryId;
        Amount = amount;
        TransactionDate = transactionDate;
        Note = string.IsNullOrWhiteSpace(note)
            ? null
            : note.Trim();
    }

    public void SoftDelete(DateTime deletedAtUtc)
    {
        if (IsDeleted)
            throw new InvalidOperationException(
                "Transaction is already deleted.");

        if (deletedAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "Deletion timestamp must be UTC.",
                nameof(deletedAtUtc));

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
    }
}
