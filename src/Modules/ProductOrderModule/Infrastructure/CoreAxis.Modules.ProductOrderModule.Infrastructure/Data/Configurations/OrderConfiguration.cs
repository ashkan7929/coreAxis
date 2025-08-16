using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.OrderNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.OrderType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Configure AssetCode value object
        builder.OwnsOne(o => o.AssetCode, ac =>
        {
            ac.Property(x => x.Value)
                .HasColumnName("AssetCode")
                .HasMaxLength(10)
                .IsRequired();
        });

        // Configure Quantity with high precision
        builder.Property(o => o.Quantity)
            .HasPrecision(18, 8)
            .IsRequired();

        // Configure LockedPrice value object
        builder.OwnsOne(o => o.LockedPrice, lp =>
        {
            lp.Property(x => x.Amount)
                .HasColumnName("LockedPriceAmount")
                .HasPrecision(18, 8);
            lp.Property(x => x.Currency)
                .HasColumnName("LockedPriceCurrency")
                .HasMaxLength(10);
        });

        // Configure TotalAmount value object
        builder.OwnsOne(o => o.TotalAmount, ta =>
        {
            ta.Property(x => x.Amount)
                .HasColumnName("TotalAmount")
                .HasPrecision(18, 8);
            ta.Property(x => x.Currency)
                .HasColumnName("TotalCurrency")
                .HasMaxLength(10);
        });

        builder.Property(o => o.IdempotencyKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.TenantId)
            .HasMaxLength(100)
            .IsRequired()
            .HasDefaultValue("default");

        builder.Property(o => o.PriceLockedAt);

        builder.Property(o => o.PriceExpiresAt);

        // Configure JsonSnapshot as JSON column
        builder.Property(o => o.JsonSnapshot)
            .HasColumnType("nvarchar(max)");

        builder.Property(o => o.Notes)
            .HasMaxLength(1000);

        // Relationships
        builder.HasMany(o => o.OrderLines)
            .WithOne(ol => ol.Order)
            .HasForeignKey(ol => ol.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(o => o.UserId)
            .HasDatabaseName("IX_Orders_UserId");

        builder.HasIndex(o => o.OrderNumber)
            .IsUnique()
            .HasDatabaseName("IX_Orders_OrderNumber");

        builder.HasIndex(o => o.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_Orders_IdempotencyKey");

        builder.HasIndex(o => new { o.UserId, o.Status })
            .HasDatabaseName("IX_Orders_UserId_Status");

        builder.HasIndex(o => o.TenantId)
            .HasDatabaseName("IX_Orders_TenantId");

        builder.HasIndex(o => o.CreatedOn)
            .HasDatabaseName("IX_Orders_CreatedOn");

        builder.HasIndex(o => o.PriceExpiresAt)
            .HasDatabaseName("IX_Orders_PriceExpiresAt")
            .HasFilter("[PriceExpiresAt] IS NOT NULL");
    }
}