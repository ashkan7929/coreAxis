using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Data.Configurations;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");

        builder.HasKey(ol => ol.Id);

        builder.Property(ol => ol.OrderId)
            .IsRequired();

        // Configure AssetCode value object
        builder.OwnsOne(ol => ol.AssetCode, ac =>
        {
            ac.Property(x => x.Value)
                .HasColumnName("OrderLineAssetCode")
                .HasMaxLength(10)
                .IsRequired();
        });

        // Configure Quantity with high precision
        builder.Property(ol => ol.Quantity)
            .HasPrecision(18, 8)
            .IsRequired();

        // Configure UnitPrice value object
        builder.OwnsOne(ol => ol.UnitPrice, up =>
        {
            up.Property(x => x.Amount)
                .HasColumnName("UnitPriceAmount")
                .HasPrecision(18, 8);
            up.Property(x => x.Currency)
                .HasColumnName("UnitPriceCurrency")
                .HasMaxLength(10);
        });

        // Configure LineTotal value object
        builder.OwnsOne(ol => ol.LineTotal, lt =>
        {
            lt.Property(x => x.Amount)
                .HasColumnName("LineTotalAmount")
                .HasPrecision(18, 8);
            lt.Property(x => x.Currency)
                .HasColumnName("LineTotalCurrency")
                .HasMaxLength(10);
        });

        builder.Property(ol => ol.Notes)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(ol => ol.Order)
            .WithMany(o => o.OrderLines)
            .HasForeignKey(ol => ol.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(ol => ol.OrderId)
            .HasDatabaseName("IX_OrderLines_OrderId");
    }
}