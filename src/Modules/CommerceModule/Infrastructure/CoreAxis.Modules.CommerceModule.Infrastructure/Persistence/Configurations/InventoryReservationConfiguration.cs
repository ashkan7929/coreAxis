using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreAxis.Modules.CommerceModule.Domain.Entities;
using CoreAxis.Modules.CommerceModule.Domain.Enums;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for InventoryReservation entity.
/// </summary>
public class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("InventoryReservations", "commerce");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .ValueGeneratedNever();
            
        builder.Property(x => x.OrderId)
            .IsRequired();
            
        builder.Property(x => x.UserId)
            .IsRequired();
            
        builder.Property(x => x.ItemsJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");
            
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();
            
        builder.Property(x => x.ExpiresAt)
            .IsRequired();
            
        builder.Property(x => x.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.IdempotencyKey)
            .IsRequired(false)
            .HasMaxLength(100);
            
        builder.Property(x => x.CreatedOn)
            .IsRequired();
            
        builder.Property(x => x.LastModifiedOn)
            .IsRequired(false);
            
        // Indexes
        builder.HasIndex(x => x.OrderId)
            .HasDatabaseName("IX_InventoryReservations_OrderId");
            
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_InventoryReservations_UserId");
            
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_InventoryReservations_Status");
            
        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_InventoryReservations_ExpiresAt");
            
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("IX_InventoryReservations_CorrelationId");
            
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_InventoryReservations_IdempotencyKey")
            .HasFilter("[IdempotencyKey] IS NOT NULL");
    }
}