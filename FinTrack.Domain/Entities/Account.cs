
using FinTrack.Domain.Common;
using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class Account
{
    public const int MaxNameLength = 100;

    public int Id { get; private set; }

    public string UserId { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public AccountType Type { get; private set; }

    public decimal OpeningBalance { get; private set; }

    public string Currency { get; private set; } = Currencies.Default;

    public bool IsArchived { get; private set; }

    private Account() { }

    public Account(
        string userId,
        string name,
        AccountType type,
        decimal openingBalance,
        string currency = Currencies.Default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(
                "User ID is required.", nameof(userId));

        if (!Enum.IsDefined(type))
            throw new ArgumentException(
                "Invalid account type.", nameof(type));

        UserId = userId;
        Name = ValidateName(name);
        Type = type;
        OpeningBalance = openingBalance;
        Currency = ValidateCurrency(currency);
        IsArchived = false;
    }

    public void Rename(string newName)
    {
        EnsureNotArchived();
        Name = ValidateName(newName);
    }

    public void ChangeType(AccountType newType)
    {
        EnsureNotArchived();

        if (!Enum.IsDefined(newType))
            throw new ArgumentException(
                "Invalid account type.", nameof(newType));

        Type = newType;
    }

    public void ChangeCurrency(string newCurrency)
    {
        EnsureNotArchived();
        Currency = ValidateCurrency(newCurrency);
    }

    public void Archive()
    {
        if (IsArchived)
            throw new InvalidOperationException(
                "Account is already archived.");

        IsArchived = true;
    }

    public void Unarchive()
    {
        if (!IsArchived)
            throw new InvalidOperationException(
                "Account is not archived.");

        IsArchived = false;
    }

    private void EnsureNotArchived()
    {
        if (IsArchived)
            throw new InvalidOperationException(
                "Archived accounts cannot be modified.");
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Account name is required.", nameof(name));

        var trimmedName = name.Trim();

        if (trimmedName.Length > MaxNameLength)
            throw new ArgumentException(
                $"Account name cannot exceed {MaxNameLength} characters.",
                nameof(name));

        return trimmedName;
    }

    private static string ValidateCurrency(string currency)
    {
        var code = currency?.Trim().ToUpperInvariant();

        if (!Currencies.IsSupported(code))
            throw new ArgumentException(
                "Unsupported currency.", nameof(currency));

        return code!;
    }
}
