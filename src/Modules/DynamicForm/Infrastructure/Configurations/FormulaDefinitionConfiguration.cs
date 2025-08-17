using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the FormulaDefinition entity.
    /// </summary>
    public class FormulaDefinitionConfiguration : IEntityTypeConfiguration<FormulaDefinition>
    {
        public void Configure(EntityTypeBuilder<FormulaDefinition> builder)
        {
            // Table configuration
            builder.ToTable("FormulaDefinitions");

            // Primary key
            builder.HasKey(fd => fd.Id);

            // Properties
            builder.Property(fd => fd.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(fd => fd.TenantId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fd => fd.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(fd => fd.Description)
                .HasMaxLength(1000);

            builder.Property(fd => fd.Category)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fd => fd.Expression)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(fd => fd.ReturnType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(fd => fd.Parameters)
                .HasColumnType("nvarchar(max)");

            builder.Property(fd => fd.Version)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("1.0.0");

            builder.Property(fd => fd.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(fd => fd.IsCurrent)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(fd => fd.PublishedAt)
                .IsRequired(false);

            builder.Property(fd => fd.PublishedBy)
                .HasMaxLength(100);

            builder.Property(fd => fd.CompiledExpression)
                .HasColumnType("nvarchar(max)");

            builder.Property(fd => fd.CompiledAt)
                .IsRequired(false);

            builder.Property(fd => fd.CompilationStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("NotCompiled");

            builder.Property(fd => fd.CompilationError)
                .HasColumnType("nvarchar(max)");

            builder.Property(fd => fd.Dependencies)
                .HasMaxLength(1000);

            builder.Property(fd => fd.Tags)
                .HasMaxLength(500);

            builder.Property(fd => fd.UsageCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(fd => fd.LastUsedAt)
                .IsRequired(false);

            builder.Property(fd => fd.Metadata)
                .HasColumnType("nvarchar(max)");

            builder.Property(fd => fd.CreatedBy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fd => fd.CreatedOn)
                .IsRequired();

            builder.Property(fd => fd.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(fd => fd.LastModifiedOn);

            builder.Property(fd => fd.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(fd => fd.RowVersion)
                .IsRowVersion();

            // Indexes
            builder.HasIndex(fd => fd.TenantId)
                .HasDatabaseName("IX_FormulaDefinitions_TenantId");

            builder.HasIndex(fd => fd.Name)
                .HasDatabaseName("IX_FormulaDefinitions_Name");

            builder.HasIndex(fd => fd.Category)
                .HasDatabaseName("IX_FormulaDefinitions_Category");

            builder.HasIndex(fd => fd.ReturnType)
                .HasDatabaseName("IX_FormulaDefinitions_ReturnType");

            builder.HasIndex(fd => fd.Version)
                .HasDatabaseName("IX_FormulaDefinitions_Version");

            builder.HasIndex(fd => fd.IsPublished)
                .HasDatabaseName("IX_FormulaDefinitions_IsPublished");

            builder.HasIndex(fd => fd.IsCurrent)
                .HasDatabaseName("IX_FormulaDefinitions_IsCurrent");

            builder.HasIndex(fd => fd.CompilationStatus)
                .HasDatabaseName("IX_FormulaDefinitions_CompilationStatus");

            builder.HasIndex(fd => fd.UsageCount)
                .HasDatabaseName("IX_FormulaDefinitions_UsageCount");

            builder.HasIndex(fd => fd.LastUsedAt)
                .HasDatabaseName("IX_FormulaDefinitions_LastUsedAt");

            builder.HasIndex(fd => new { fd.TenantId, fd.Name })
                .IsUnique()
                .HasDatabaseName("IX_FormulaDefinitions_TenantId_Name_Unique");

            builder.HasIndex(fd => new { fd.TenantId, fd.Category, fd.IsPublished })
                .HasDatabaseName("IX_FormulaDefinitions_TenantId_Category_IsPublished");

            builder.HasIndex(fd => new { fd.TenantId, fd.IsCurrent })
                .HasDatabaseName("IX_FormulaDefinitions_TenantId_IsCurrent")
                .HasFilter("[IsCurrent] = 1");

            // Relationships
            builder.HasMany(fd => fd.EvaluationLogs)
                .WithOne(fel => fel.FormulaDefinition)
                .HasForeignKey(fel => fel.FormulaDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore domain events
            builder.Ignore(fd => fd.DomainEvents);
        }
    }
}