using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the ProductFormulaBinding entity.
    /// </summary>
    public class ProductFormulaBindingConfiguration : IEntityTypeConfiguration<ProductFormulaBinding>
    {
        public void Configure(EntityTypeBuilder<ProductFormulaBinding> builder)
        {
            // Table configuration
            builder.ToTable("ProductFormulaBindings");

            // Primary key
            builder.HasKey(pfb => pfb.Id);

            // Properties
            builder.Property(pfb => pfb.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(pfb => pfb.TenantId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(pfb => pfb.ProductId)
                .IsRequired();

            builder.Property(pfb => pfb.FormulaDefinitionId)
                .IsRequired();

            builder.Property(pfb => pfb.VersionNumber)
                .IsRequired();

            // Unique index to ensure a product binds to a single version at a time
            builder.HasIndex(pfb => new { pfb.ProductId, pfb.VersionNumber })
                   .IsUnique()
                   .HasDatabaseName("UX_ProductFormulaBinding_ProductId_Version");

            // Relationships
            builder.HasOne<FormulaDefinition>()
                   .WithMany()
                   .HasForeignKey(pfb => pfb.FormulaDefinitionId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Ignore domain events
            builder.Ignore(pfb => pfb.DomainEvents);
        }
    }
}