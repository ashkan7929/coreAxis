using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreAxis.Modules.MLMModule.Domain.Entities;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Data.Configurations;

public class CommissionRuleVersionConfiguration : IEntityTypeConfiguration<CommissionRuleVersion>
{
    public void Configure(EntityTypeBuilder<CommissionRuleVersion> builder)
    {
        builder.ToTable("CommissionRuleVersions");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.RuleSetId)
            .IsRequired();
            
        builder.Property(x => x.Version)
            .IsRequired();
            
        builder.Property(x => x.SchemaJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");
            
        builder.Property(x => x.IsPublished)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(x => x.PublishedAt)
            .IsRequired(false);
            
        builder.Property(x => x.PublishedBy)
            .IsRequired(false)
            .HasMaxLength(100);
            
        builder.Property(x => x.CreatedOn)
            .IsRequired();
            
        builder.Property(x => x.LastModifiedOn)
            .IsRequired(false);
        
        // Indexes
        builder.HasIndex(x => new { x.RuleSetId, x.Version })
            .IsUnique()
            .HasDatabaseName("IX_CommissionRuleVersions_RuleSetId_Version");
            
        builder.HasIndex(x => x.IsPublished)
            .HasDatabaseName("IX_CommissionRuleVersions_IsPublished");
            
        builder.HasIndex(x => x.PublishedAt)
            .HasDatabaseName("IX_CommissionRuleVersions_PublishedAt");
        
        // Relationships
        builder.HasOne(x => x.RuleSet)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.RuleSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}