using CoreAxis.Modules.WalletModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Data.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.UserId)
            .IsRequired();

        builder.Property(w => w.WalletTypeId)
            .IsRequired();

        builder.Property(w => w.Balance)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(w => w.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(w => w.IsLocked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(w => w.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Relationships
        builder.HasOne(w => w.WalletType)
            .WithMany(wt => wt.Wallets)
            .HasForeignKey(w => w.WalletTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Transactions)
            .WithOne(t => t.Wallet)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.WalletContracts)
            .WithOne(wc => wc.Wallet)
            .HasForeignKey(wc => wc.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(w => w.UserId)
            .HasDatabaseName("IX_Wallets_UserId");

        builder.HasIndex(w => new { w.UserId, w.WalletTypeId })
            .IsUnique()
            .HasDatabaseName("IX_Wallets_UserId_WalletTypeId");

        builder.HasIndex(w => w.WalletTypeId)
            .HasDatabaseName("IX_Wallets_WalletTypeId");
    }
}