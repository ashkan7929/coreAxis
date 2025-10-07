using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Data.Configurations
{
    public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
    {
        public void Configure(EntityTypeBuilder<Quote> builder)
        {
            // Table & schema will be set by DbContext default schema

            builder.HasKey(q => q.Id);

            builder.Property(q => q.ProductId)
                   .IsRequired();

            builder.Property(q => q.ExpiresAt)
                   .IsRequired();

            builder.Property(q => q.Consumed)
                   .HasDefaultValue(false)
                   .IsRequired();

            builder.Property(q => q.PricingJson)
                   .HasColumnType("nvarchar(max)")
                   .IsRequired();

            builder.Property(q => q.InputsSnapshotJson)
                   .HasColumnType("nvarchar(max)")
                   .IsRequired();

            builder.Property(q => q.ExternalDataSnapshotJson)
                   .HasColumnType("nvarchar(max)")
                   .IsRequired();

            // Single-field indexes for common queries
            builder.HasIndex(q => q.ProductId).HasDatabaseName("IX_Quote_ProductId");
            builder.HasIndex(q => q.ExpiresAt).HasDatabaseName("IX_Quote_ExpiresAt");
            builder.HasIndex(q => q.Consumed).HasDatabaseName("IX_Quote_Consumed");

            // Composite index to efficiently fetch valid quotes per product
            builder.HasIndex(q => new { q.ProductId, q.Consumed, q.ExpiresAt })
                   .HasDatabaseName("IX_Quote_Product_Consumed_Expires");
        }
    }
}