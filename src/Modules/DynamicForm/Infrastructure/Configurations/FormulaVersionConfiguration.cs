using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreAxis.Modules.DynamicForm.Domain.Entities;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations;

public class FormulaVersionConfiguration : IEntityTypeConfiguration<FormulaVersion>
{
    public void Configure(EntityTypeBuilder<FormulaVersion> builder)
    {
        builder.ToTable("FormulaVersions", "DynamicForm");

        // Primary Key
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Properties
        builder.Property(x => x.FormulaDefinitionId)
            .IsRequired();

        builder.Property(x => x.VersionNumber)
            .IsRequired();

        builder.Property(x => x.Expression)
            .IsRequired()
            .HasMaxLength(10000);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.ChangeLog)
            .HasMaxLength(2000);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsPublished)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.PublishedAt)
            .IsRequired(false);

        builder.Property(x => x.PublishedBy)
            .IsRequired(false);

        builder.Property(x => x.ValidationRules)
            .HasMaxLength(5000);

        builder.Property(x => x.Dependencies)
            .HasMaxLength(2000);

        builder.Property(x => x.Metadata)
            .HasMaxLength(5000);

        builder.Property(x => x.ExecutionCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastExecutedAt)
            .IsRequired(false);

        builder.Property(x => x.AverageExecutionTime)
            .IsRequired(false)
            .HasPrecision(18, 6);

        builder.Property(x => x.LastError)
            .HasMaxLength(2000);

        builder.Property(x => x.LastErrorAt)
            .IsRequired(false);

        // Base Entity Properties
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .IsRequired(false);

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(256);

        builder.Property(x => x.TenantId)
            .IsRequired()
            .HasMaxLength(256);

        // Relationships
        builder.HasOne(x => x.FormulaDefinition)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.FormulaDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.EvaluationLogs)
            .WithOne(x => x.FormulaVersion)
            .HasForeignKey(x => x.FormulaVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.FormulaDefinitionId)
            .HasDatabaseName("IX_FormulaVersions_FormulaDefinitionId");

        builder.HasIndex(x => new { x.FormulaDefinitionId, x.VersionNumber })
            .IsUnique()
            .HasDatabaseName("IX_FormulaVersions_FormulaDefinitionId_VersionNumber");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_FormulaVersions_IsActive")
            .HasFilter("[IsActive] = 1");

        builder.HasIndex(x => x.IsPublished)
            .HasDatabaseName("IX_FormulaVersions_IsPublished")
            .HasFilter("[IsPublished] = 1");

        builder.HasIndex(x => x.PublishedAt)
            .HasDatabaseName("IX_FormulaVersions_PublishedAt");

        builder.HasIndex(x => x.LastExecutedAt)
            .HasDatabaseName("IX_FormulaVersions_LastExecutedAt");

        builder.HasIndex(x => x.TenantId)
            .HasDatabaseName("IX_FormulaVersions_TenantId");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_FormulaVersions_CreatedAt");

        // Composite Indexes for Performance
        builder.HasIndex(x => new { x.TenantId, x.FormulaDefinitionId, x.IsActive })
            .HasDatabaseName("IX_FormulaVersions_TenantId_FormulaDefinitionId_IsActive");

        builder.HasIndex(x => new { x.TenantId, x.IsPublished, x.PublishedAt })
            .HasDatabaseName("IX_FormulaVersions_TenantId_IsPublished_PublishedAt");

        builder.HasIndex(x => new { x.FormulaDefinitionId, x.IsActive, x.VersionNumber })
            .HasDatabaseName("IX_FormulaVersions_FormulaDefinitionId_IsActive_VersionNumber");

        // Check Constraints
        builder.HasCheckConstraint("CK_FormulaVersions_VersionNumber", "[VersionNumber] > 0");
        builder.HasCheckConstraint("CK_FormulaVersions_ExecutionCount", "[ExecutionCount] >= 0");
        builder.HasCheckConstraint("CK_FormulaVersions_AverageExecutionTime", "[AverageExecutionTime] IS NULL OR [AverageExecutionTime] >= 0");
        builder.HasCheckConstraint("CK_FormulaVersions_PublishedConstraint", "([IsPublished] = 0) OR ([IsPublished] = 1 AND [PublishedAt] IS NOT NULL AND [PublishedBy] IS NOT NULL)");
        builder.HasCheckConstraint("CK_FormulaVersions_ActiveConstraint", "([IsActive] = 0) OR ([IsActive] = 1 AND [IsPublished] = 1)");

        // Ignore Domain Events
        builder.Ignore(x => x.DomainEvents);
    }
}