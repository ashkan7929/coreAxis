using CoreAxis.Modules.WalletModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Data.Configurations;

public class WalletContractConfiguration : IEntityTypeConfiguration<WalletContract>
{
    public void Configure(EntityTypeBuilder<WalletContract> builder)
    {
        builder.ToTable("WalletContracts");

        builder.HasKey(wc => wc.Id);

        builder.Property(wc => wc.UserId)
            .IsRequired();

        builder.Property(wc => wc.WalletId)
            .IsRequired();

        builder.Property(wc => wc.ProviderId)
            .IsRequired();

        builder.Property(wc => wc.MaxAmount)
            .HasColumnType("decimal(18,8)")
            .IsRequired();

        builder.Property(wc => wc.DailyLimit)
            .HasColumnType("decimal(18,8)")
            .IsRequired();

        builder.Property(wc => wc.MonthlyLimit)
            .HasColumnType("decimal(18,8)")
            .IsRequired();

        builder.Property(wc => wc.UsedDailyAmount)
            .HasColumnType("decimal(18,8)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(wc => wc.UsedMonthlyAmount)
            .HasColumnType("decimal(18,8)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(wc => wc.LastResetDate)
            .IsRequired();

        builder.Property(wc => wc.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(wc => wc.Terms)
            .HasColumnType("nvarchar(max)");

        // Relationships
        builder.HasOne(wc => wc.Wallet)
            .WithMany(w => w.WalletContracts)
            .HasForeignKey(wc => wc.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(wc => wc.Provider)
            .WithMany(wp => wp.WalletContracts)
            .HasForeignKey(wc => wc.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(wc => wc.UserId)
            .HasDatabaseName("IX_WalletContracts_UserId");

        builder.HasIndex(wc => wc.WalletId)
            .HasDatabaseName("IX_WalletContracts_WalletId");

        builder.HasIndex(wc => wc.ProviderId)
            .HasDatabaseName("IX_WalletContracts_ProviderId");

        builder.HasIndex(wc => new { wc.WalletId, wc.ProviderId })
            .IsUnique()
            .HasDatabaseName("IX_WalletContracts_WalletId_ProviderId");
    }
}