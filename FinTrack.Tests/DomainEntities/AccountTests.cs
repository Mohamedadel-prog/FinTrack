
using FinTrack.Domain.Entities;

namespace FinTrack.Tests.Domain.Entities;

public class AccountTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateAccount()
    {
        // Arrange
        string userId = "user-123";
        string name = "Cash";
        decimal openingBalance = 3000m;

        // Act
        var account = new Account(userId, name, openingBalance);

        // Assert
        Assert.Equal(userId, account.UserId);
        Assert.Equal(name, account.Name);
        Assert.Equal(openingBalance, account.OpeningBalance);
        Assert.Equal("EGP", account.Currency);
        Assert.False(account.IsArchived);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ShouldThrowException(string? name)
    {
        Assert.Throws<ArgumentException>(() =>
            new Account("user-123", name!, 1000m));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithInvalidUserId_ShouldThrowException(string? userId)
    {
        Assert.Throws<ArgumentException>(() =>
            new Account(userId!, "Cash", 1000m));
    }

    [Fact]
    public void Rename_WithValidName_ShouldUpdateName()
    {
        var account = new Account("user-123", "Cash", 1000m);

        account.Rename("Bank Account");

        Assert.Equal("Bank Account", account.Name);
    }

    [Fact]
    public void Archive_ShouldSetIsArchivedToTrue()
    {
        var account = new Account("user-123", "Cash", 1000m);

        account.Archive();

        Assert.True(account.IsArchived);
    }
}
