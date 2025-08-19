using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreAxis.Modules.MLMModule.Domain.Entities;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Data.Configurations;

public class ProductRuleBindingConfiguration : IEntityTypeConfiguration<ProductRuleBinding>
{
    public void Configure(EntityTypeBuilder<ProductRuleBinding> builder)
    {
        builder.ToTable("ProductRuleBindings");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.ProductId)
            .IsRequired();
            
        builder.Property(x => x.CommissionRuleSetId)
            .IsRequired();
            
        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(x => x.ValidFrom)
            .IsRequired(false);
            
        builder.Property(x => x.ValidTo)
            .IsRequired(false);
            
        builder.Property(x => x.CreatedOn)
            .IsRequired();
            
        builder.Property(x => x.LastModifiedOn)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(x => x.ProductId)
            .HasDatabaseName("IX_ProductRuleBindings_ProductId");
            
        builder.HasIndex(x => x.CommissionRuleSetId)
            .HasDatabaseName("IX_ProductRuleBindings_CommissionRuleSetId");
            
        builder.HasIndex(x => new { x.ProductId, x.CommissionRuleSetId })
            .HasDatabaseName("IX_ProductRuleBindings_ProductId_RuleSetId");
            
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_ProductRuleBindings_IsActive");
            
        builder.HasIndex(x => new { x.ValidFrom, x.ValidTo })
            .HasDatabaseName("IX_ProductRuleBindings_ValidPeriod");
        
        // Relationships
        builder.HasOne(x => x.CommissionRuleSet)
            .WithMany(x => x.ProductBindings)
            .HasForeignKey(x => x.CommissionRuleSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}