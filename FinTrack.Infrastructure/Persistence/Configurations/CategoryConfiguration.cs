
using FinTrack.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrack.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration
    : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(Category.MaxNameLength);

        builder.Property(c => c.UserId)
            .IsRequired();

        builder.HasIndex(c => new
        {
            c.UserId,
            c.Type,
            c.Name
        })
            .IsUnique()
            .HasFilter("[IsArchived] = 0");


        builder.Property(c => c.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(c => c.IsArchived)
            .HasDefaultValue(false);

        builder.HasIndex(c => c.UserId);

        builder.HasOne<FinTrack.Infrastructure.Identity.ApplicationUser>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
