using CoreAxis.Modules.ProductOrderModule.Domain.Entities;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        // Keys
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        // Quantity precision (align with migration: decimal(18,2))
        builder.Property(p => p.Quantity)
            .HasPrecision(18, 2);

        // Status enum as string with max length
        var statusConverter = new EnumToStringConverter<ProductStatus>();
        builder.Property(p => p.Status)
            .HasConversion(statusConverter)
            .HasMaxLength(20)
            .IsRequired();

        // Optional SupplierId FK
        builder.Property(p => p.SupplierId)
            .IsRequired(false);

        builder.HasOne<Supplier>()
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        // Owned value object: Money for PriceFrom
        builder.OwnsOne(p => p.PriceFrom, money =>
        {
            money.Property(m => m.Amount)
                .HasPrecision(18, 8)
                .HasColumnName("PriceFromAmount");

            money.Property(m => m.Currency)
                .HasMaxLength(10)
                .HasColumnName("PriceFromCurrency");

            money.WithOwner();
        });

        // Attributes dictionary as JSON
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var dictToJsonConverter = new ValueConverter<Dictionary<string, string>?, string?>(
            v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
            v => string.IsNullOrWhiteSpace(v) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(v!, jsonOptions)!);

        builder.Property(p => p.Attributes)
            .HasConversion(dictToJsonConverter)
            .HasColumnType("nvarchar(max)")
            .HasColumnName("AttributesJson");

        // Indexes
        builder.HasIndex(p => p.Code)
            .IsUnique()
            .HasDatabaseName("IX_Products_Code");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("IX_Products_Status");

        builder.HasIndex(p => p.Name)
            .HasDatabaseName("IX_Products_Name");

        builder.HasIndex(p => p.SupplierId)
            .HasDatabaseName("IX_Products_SupplierId");
    }
}