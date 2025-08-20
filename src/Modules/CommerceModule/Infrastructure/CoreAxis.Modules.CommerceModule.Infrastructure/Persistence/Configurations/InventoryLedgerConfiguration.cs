using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for InventoryLedger entity.
/// </summary>
public class InventoryLedgerConfiguration : IEntityTypeConfiguration<InventoryLedger>
{
    public void Configure(EntityTypeBuilder<InventoryLedger> builder)
    {
        builder.ToTable("InventoryLedger", "commerce");
        
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
            
        builder.Property(x => x.QuantityChange)
            .HasPrecision(18, 4)
            .IsRequired();
            
        builder.Property(x => x.QuantityBefore)
            .HasPrecision(18, 4)
            .IsRequired();
            
        builder.Property(x => x.QuantityAfter)
            .HasPrecision(18, 4)
            .IsRequired();
            
        builder.Property(x => x.Reason)
            .IsRequired()
            .HasConversion<int>();
            
        builder.Property(x => x.ReferenceId)
            .IsRequired(false);
            
        builder.Property(x => x.Notes)
            .IsRequired(false)
            .HasMaxLength(500);
            
        builder.Property(x => x.CorrelationId)
            .IsRequired(false)
            .HasMaxLength(100);
            
        builder.Property(x => x.CreatedOn)
            .IsRequired();
            
        builder.Property(x => x.LastModifiedOn)
            .IsRequired(false);
            
        // Indexes
        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("IX_InventoryLedger_ProductId");
            
        builder.HasIndex(x => x.Sku)
            .HasDatabaseName("IX_InventoryLedger_Sku");
            
        builder.HasIndex(x => x.LocationId)
            .HasDatabaseName("IX_InventoryLedger_LocationId");
            
        builder.HasIndex(x => x.Reason)
            .HasDatabaseName("IX_InventoryLedger_Reason");
            
        builder.HasIndex(x => x.ReferenceId)
            .HasDatabaseName("IX_InventoryLedger_ReferenceId");
            
        builder.HasIndex(x => x.CreatedOn)
            .HasDatabaseName("IX_InventoryLedger_CreatedOn");
            
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_InventoryLedger_CorrelationId");
    }
}