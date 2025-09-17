using CoreAxis.Modules.CommerceModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Data.Configurations;

public class InventoryLedgerConfiguration : IEntityTypeConfiguration<InventoryLedger>
{
    public void Configure(EntityTypeBuilder<InventoryLedger> builder)
    {
        builder.ToTable("InventoryLedgers", "commerce");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.InventoryItemId)
            .IsRequired();

        builder.Property(x => x.TransactionType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.TotalValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.BalanceAfter)
            .IsRequired();

        builder.Property(x => x.ReferenceId)
            .HasMaxLength(100);

        builder.Property(x => x.ReferenceType)
            .HasMaxLength(50);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(x => x.InventoryItemId)
            .HasDatabaseName("IX_InventoryLedgers_InventoryItemId");

        builder.HasIndex(x => x.TransactionType)
            .HasDatabaseName("IX_InventoryLedgers_TransactionType");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_InventoryLedgers_CreatedAt");

        builder.HasIndex(x => new { x.ReferenceId, x.ReferenceType })
            .HasDatabaseName("IX_InventoryLedgers_Reference");

        // Relationships
        builder.HasOne(x => x.InventoryItem)
            .WithMany(x => x.LedgerEntries)
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}