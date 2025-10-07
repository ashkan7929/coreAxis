using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
    {
        public void Configure(EntityTypeBuilder<Quote> builder)
        {
            builder.ToTable("Quotes");

            builder.HasKey(q => q.Id);
            builder.Property(q => q.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(q => q.ProductId)
                .IsRequired();

            builder.Property(q => q.ExpiresAt)
                .IsRequired();

            builder.Property(q => q.Consumed)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(q => q.PricingJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(q => q.InputsSnapshotJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(q => q.ExternalDataSnapshotJson)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            // Indexes
            builder.HasIndex(q => q.ExpiresAt)
                .HasDatabaseName("IX_Quotes_ExpiresAt");

            builder.HasIndex(q => q.Consumed)
                .HasDatabaseName("IX_Quotes_Consumed");

            // Ignore domain events
            builder.Ignore(q => q.DomainEvents);
        }
    }
}