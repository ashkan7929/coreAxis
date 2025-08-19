using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreAxis.Modules.MLMModule.Domain.Entities;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Data.Configurations;

public class CommissionLevelConfiguration : IEntityTypeConfiguration<CommissionLevel>
{
    public void Configure(EntityTypeBuilder<CommissionLevel> builder)
    {
        builder.ToTable("CommissionLevels");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.CommissionRuleSetId)
            .IsRequired();
            
        builder.Property(x => x.Level)
            .IsRequired();
            
        builder.Property(x => x.Percentage)
            .IsRequired()
            .HasColumnType("decimal(5,4)"); // Supports up to 99.9999%
            
        builder.Property(x => x.FixedAmount)
            .IsRequired(false)
            .HasColumnType("decimal(18,6)");
            
        builder.Property(x => x.MaxAmount)
            .IsRequired(false)
            .HasColumnType("decimal(18,6)");
            
        builder.Property(x => x.MinAmount)
            .IsRequired(false)
            .HasColumnType("decimal(18,6)");
            
        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(x => x.CreatedOn)
            .IsRequired();
            
        builder.Property(x => x.LastModifiedOn)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(x => new { x.CommissionRuleSetId, x.Level })
            .IsUnique()
            .HasDatabaseName("IX_CommissionLevels_RuleSetId_Level");
            
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_CommissionLevels_IsActive");
        
        // Relationships
        builder.HasOne(x => x.CommissionRuleSet)
            .WithMany(x => x.CommissionLevels)
            .HasForeignKey(x => x.CommissionRuleSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}