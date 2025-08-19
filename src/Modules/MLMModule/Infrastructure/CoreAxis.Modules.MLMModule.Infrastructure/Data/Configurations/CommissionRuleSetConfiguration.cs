using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreAxis.Modules.MLMModule.Domain.Entities;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Data.Configurations;

public class CommissionRuleSetConfiguration : IEntityTypeConfiguration<CommissionRuleSet>
{
    public void Configure(EntityTypeBuilder<CommissionRuleSet> builder)
    {
        builder.ToTable("CommissionRuleSets");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(1000);
            
        builder.Property(x => x.LatestVersion)
            .IsRequired()
            .HasDefaultValue(0);
            
        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(x => x.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(x => x.MaxLevels)
            .IsRequired()
            .HasDefaultValue(10);
            
        builder.Property(x => x.MinimumPurchaseAmount)
            .IsRequired()
            .HasColumnType("decimal(18,6)")
            .HasDefaultValue(0);
            
        builder.Property(x => x.RequireActiveUpline)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(x => x.CreatedOn)
            .IsRequired();
            
        builder.Property(x => x.LastModifiedOn)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("IX_CommissionRuleSets_Code");
            
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_CommissionRuleSets_IsActive");
            
        builder.HasIndex(x => x.IsDefault)
            .HasDatabaseName("IX_CommissionRuleSets_IsDefault");
        
        // Relationships
        builder.HasMany(x => x.CommissionLevels)
            .WithOne(x => x.CommissionRuleSet)
            .HasForeignKey(x => x.CommissionRuleSetId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(x => x.Versions)
            .WithOne(x => x.RuleSet)
            .HasForeignKey(x => x.RuleSetId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(x => x.ProductBindings)
            .WithOne(x => x.CommissionRuleSet)
            .HasForeignKey(x => x.CommissionRuleSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}