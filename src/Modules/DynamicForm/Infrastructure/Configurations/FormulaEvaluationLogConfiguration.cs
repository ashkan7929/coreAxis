using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the FormulaEvaluationLog entity.
    /// </summary>
    public class FormulaEvaluationLogConfiguration : IEntityTypeConfiguration<FormulaEvaluationLog>
    {
        public void Configure(EntityTypeBuilder<FormulaEvaluationLog> builder)
        {
            // Table configuration
            builder.ToTable("FormulaEvaluationLogs");

            // Primary key
            builder.HasKey(fel => fel.Id);

            // Properties
            builder.Property(fel => fel.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(fel => fel.FormulaDefinitionId)
                .IsRequired();

            builder.Property(fel => fel.FormulaVersionId)
                .IsRequired(false);

            builder.Property(fel => fel.FormId)
                .IsRequired(false);

            builder.Property(fel => fel.FormSubmissionId)
                .IsRequired(false);

            builder.Property(fel => fel.TenantId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fel => fel.EvaluationContext)
                .IsRequired()
                .HasMaxLength(100);



            builder.Property(fel => fel.Result)
                .HasColumnType("nvarchar(max)");

            // ResultType property removed as it doesn't exist

            builder.Property(fel => fel.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            builder.Property(fel => fel.ErrorMessage)
                .HasMaxLength(1000);

            builder.Property(fel => fel.ErrorDetails)
                .HasColumnType("nvarchar(max)");

            builder.Property(fel => fel.ExecutionTimeMs)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(fel => fel.StartedAt)
                .IsRequired();

            builder.Property(fel => fel.CompletedAt)
                .IsRequired(false);

            builder.Property(fel => fel.UserId)
                .HasMaxLength(100);

            builder.Property(fel => fel.SessionId)
                .HasMaxLength(100);

            builder.Property(fel => fel.CorrelationId)
                .HasMaxLength(100);

            builder.Property(fel => fel.MemoryUsageBytes)
                .IsRequired()
                .HasDefaultValue(0);

            // EvaluationSteps and CpuUsagePercent properties removed as they don't exist

            builder.Property(fel => fel.Metadata)
                .HasColumnType("nvarchar(max)");

            builder.Property(fel => fel.CreatedBy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fel => fel.CreatedOn)
                .IsRequired();

            builder.Property(fel => fel.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(fel => fel.LastModifiedOn);

            builder.Property(fel => fel.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // RowVersion property removed as it doesn't exist

            // Indexes
            builder.HasIndex(fel => fel.FormulaDefinitionId)
                .HasDatabaseName("IX_FormulaEvaluationLogs_FormulaDefinitionId");

            builder.HasIndex(fel => fel.FormId)
                .HasDatabaseName("IX_FormulaEvaluationLogs_FormId");

            builder.Property(fel => fel.FormSubmissionId)
                .IsRequired(false);

            builder.HasIndex(fel => fel.FormSubmissionId)
                .HasDatabaseName("IX_FormulaEvaluationLogs_FormSubmissionId");

            builder.HasIndex(fel => fel.TenantId)
                .HasDatabaseName("IX_FormulaEvaluationLogs_TenantId");

            builder.HasIndex(fel => fel.EvaluationContext)
                .HasDatabaseName("IX_FormulaEvaluationLogs_EvaluationContext");

            builder.HasIndex(fel => fel.Status)
                .HasDatabaseName("IX_FormulaEvaluationLogs_Status");

            builder.HasIndex(fel => fel.StartedAt)
                .HasDatabaseName("IX_FormulaEvaluationLogs_StartedAt");

            builder.HasIndex(fel => fel.CompletedAt)
                .HasDatabaseName("IX_FormulaEvaluationLogs_CompletedAt");

            builder.HasIndex(fel => fel.ExecutionTimeMs)
                .HasDatabaseName("IX_FormulaEvaluationLogs_ExecutionTimeMs");

            builder.HasIndex(fel => fel.UserId)
                .HasDatabaseName("IX_FormulaEvaluationLogs_UserId");

            builder.HasIndex(fel => fel.SessionId)
                .HasDatabaseName("IX_FormulaEvaluationLogs_SessionId");

            builder.HasIndex(fel => fel.CorrelationId)
                .HasDatabaseName("IX_FormulaEvaluationLogs_CorrelationId");

            builder.HasIndex(fel => new { fel.TenantId, fel.StartedAt })
                .HasDatabaseName("IX_FormulaEvaluationLogs_TenantId_StartedAt");

            builder.HasIndex(fel => new { fel.FormulaDefinitionId, fel.Status, fel.StartedAt })
                .HasDatabaseName("IX_FormulaEvaluationLogs_FormulaDefinitionId_Status_StartedAt");

            builder.HasIndex(fel => new { fel.UserId, fel.StartedAt })
                .HasDatabaseName("IX_FormulaEvaluationLogs_UserId_StartedAt");

            // Relationships
            builder.HasOne(fel => fel.FormulaDefinition)
                .WithMany(fd => fd.EvaluationLogs)
                .HasForeignKey(fel => fel.FormulaDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(fel => fel.FormulaVersion)
                .WithMany(fv => fv.EvaluationLogs)
                .HasForeignKey(fel => fel.FormulaVersionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(fel => fel.Form)
                .WithMany()
                .HasForeignKey(fel => fel.FormId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(fel => fel.FormSubmission)
                .WithMany()
                .HasForeignKey(fel => fel.FormSubmissionId)
                .OnDelete(DeleteBehavior.NoAction);

            // Ignore domain events
            builder.Ignore(fel => fel.DomainEvents);
        }
    }
}