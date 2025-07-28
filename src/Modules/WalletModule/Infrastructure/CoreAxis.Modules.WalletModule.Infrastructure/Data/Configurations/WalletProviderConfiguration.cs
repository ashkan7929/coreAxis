using CoreAxis.Modules.WalletModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Data.Configurations;

public class WalletProviderConfiguration : IEntityTypeConfiguration<WalletProvider>
{
    public void Configure(EntityTypeBuilder<WalletProvider> builder)
    {
        builder.ToTable("WalletProviders");

        builder.HasKey(wp => wp.Id);

        builder.Property(wp => wp.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(wp => wp.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(wp => wp.ApiUrl)
            .HasMaxLength(500);

        builder.Property(wp => wp.SupportsDeposit)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(wp => wp.SupportsWithdraw)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(wp => wp.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(wp => wp.Configuration)
            .HasColumnType("nvarchar(max)");

        // Relationships
        builder.HasMany(wp => wp.WalletContracts)
            .WithOne(wc => wc.Provider)
            .HasForeignKey(wc => wc.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(wp => wp.Name)
            .IsUnique()
            .HasDatabaseName("IX_WalletProviders_Name");

        builder.HasIndex(wp => wp.Type)
            .HasDatabaseName("IX_WalletProviders_Type");
    }
}