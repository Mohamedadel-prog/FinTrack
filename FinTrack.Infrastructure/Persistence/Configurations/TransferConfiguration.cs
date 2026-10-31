
using FinTrack.Domain.Entities;
using FinTrack.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

public class TransferConfiguration
    : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("Transfers", t =>
        {
            t.HasCheckConstraint(
                "CK_Transfers_Amount_Positive",
                "[Amount] > 0");

            t.HasCheckConstraint(
                "CK_Transfers_DifferentAccounts",
                "[FromAccountId] <> [ToAccountId]");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.TransferDate)
            .IsRequired();

        builder.Property(t => t.Note)
            .HasMaxLength(Transfer.MaxNoteLength);

        builder.Property(t => t.IsDeleted)
            .HasDefaultValue(false);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(t => t.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(t => t.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new
        {
            t.UserId,
            t.TransferDate
        });
    }
}
