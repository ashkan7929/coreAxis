using CoreAxis.Modules.CommerceModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Data.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems", "commerce");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.SKU)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.Quantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ReservedQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.AvailableQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.UnitPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CostPrice)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MinimumStockLevel)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.MaximumStockLevel)
            .HasDefaultValue(0);

        builder.Property(x => x.ReorderPoint)
            .HasDefaultValue(0);

        builder.Property(x => x.Location)
            .HasMaxLength(200);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("IX_InventoryItems_ProductId");

        builder.HasIndex(x => x.SKU)
            .IsUnique()
            .HasDatabaseName("IX_InventoryItems_SKU");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_InventoryItems_IsActive");

        // Relationships
        builder.HasMany(x => x.LedgerEntries)
            .WithOne(x => x.InventoryItem)
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}