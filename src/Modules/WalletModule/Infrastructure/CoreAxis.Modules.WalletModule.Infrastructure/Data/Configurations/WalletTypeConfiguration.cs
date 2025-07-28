using CoreAxis.Modules.WalletModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Data.Configurations;

public class WalletTypeConfiguration : IEntityTypeConfiguration<WalletType>
{
    public void Configure(EntityTypeBuilder<WalletType> builder)
    {
        builder.ToTable("WalletTypes");

        builder.HasKey(wt => wt.Id);

        builder.Property(wt => wt.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(wt => wt.Description)
            .HasMaxLength(500);

        builder.Property(wt => wt.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasMany(wt => wt.Wallets)
            .WithOne(w => w.WalletType)
            .HasForeignKey(w => w.WalletTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(wt => wt.Name)
            .IsUnique()
            .HasDatabaseName("IX_WalletTypes_Name");
    }
}