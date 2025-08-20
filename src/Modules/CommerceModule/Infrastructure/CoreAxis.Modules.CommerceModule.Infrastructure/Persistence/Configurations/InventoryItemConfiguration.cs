using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreAxis.Modules.CommerceModule.Domain.Entities;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for InventoryItem entity.
/// </summary>
public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems", "commerce");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedNever();
            
        builder.Property(x => x.ProductId)
            .IsRequired();
            
        builder.Property(x => x.Sku)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.LocationId)
            .IsRequired(false);
            
        builder.Property(x => x.OnHand)
            .HasPrecision(18, 4)
            .IsRequired();
            
        builder.Property(x => x.Reserved)
            .HasPrecision(18, 4)
            .IsRequired();
            
        builder.Property(x => x.ReorderThreshold)
            .HasPrecision(18, 4)
            .IsRequired();
            
        builder.Property(x => x.IsTracked)
            .IsRequired();
            
        builder.Property(x => x.CreatedOn)
            .IsRequired();
            
        builder.Property(x => x.LastModifiedOn)
            .IsRequired(false);
            
        // Indexes
        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("IX_InventoryItems_ProductId");
            
        builder.HasIndex(x => x.Sku)
            .HasDatabaseName("IX_InventoryItems_Sku");
            
        builder.HasIndex(x => new { x.ProductId, x.LocationId })
            .IsUnique()
            .HasDatabaseName("IX_InventoryItems_ProductId_LocationId");
            
        builder.HasIndex(x => x.LocationId)
            .HasDatabaseName("IX_InventoryItems_LocationId");
    }
}