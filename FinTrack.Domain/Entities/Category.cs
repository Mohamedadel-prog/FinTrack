
using FinTrack.Domain.Enums;

namespace FinTrack.Domain.Entities;

public class Category
{
    public const int MaxNameLength = 100;

    public int Id { get; private set; }

    public string UserId { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public CategoryType Type { get; private set; }

    public bool IsArchived { get; private set; }

    private Category() { }

    public Category(
        string userId,
        string name,
        CategoryType type)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(
                "User ID is required.", nameof(userId));

        if (!Enum.IsDefined(type))
            throw new ArgumentException(
                "Invalid category type.", nameof(type));

        UserId = userId;
        Name = ValidateName(name);
        Type = type;
        IsArchived = false;
    }

    public void Rename(string newName)
    {
        EnsureNotArchived();

        Name = ValidateName(newName);
    }

    public void Archive()
    {
        if (IsArchived)
            throw new InvalidOperationException(
                "Category is already archived.");

        IsArchived = true;
    }

    private void EnsureNotArchived()
    {
        if (IsArchived)
            throw new InvalidOperationException(
                "Archived categories cannot be modified.");
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Category name is required.", nameof(name));

        var trimmedName = name.Trim();

        if (trimmedName.Length > MaxNameLength)
            throw new ArgumentException(
                $"Category name cannot exceed {MaxNameLength} characters.",
                nameof(name));

        return trimmedName;
    }
}
