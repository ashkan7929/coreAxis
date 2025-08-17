using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the FormAuditLog entity.
    /// </summary>
    public class FormAuditLogConfiguration : IEntityTypeConfiguration<FormAuditLog>
    {
        public void Configure(EntityTypeBuilder<FormAuditLog> builder)
        {
            // Table configuration
            builder.ToTable("FormAuditLogs");

            // Primary key
            builder.HasKey(fal => fal.Id);

            // Properties
            builder.Property(fal => fal.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(fal => fal.FormId)
                .IsRequired(false);

            builder.Property(fal => fal.FormSubmissionId)
                .IsRequired(false);

            builder.Property(fal => fal.TenantId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fal => fal.Action)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(fal => fal.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fal => fal.EntityId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fal => fal.UserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fal => fal.UserName)
                .HasMaxLength(200);

            builder.Property(fal => fal.IpAddress)
                .HasMaxLength(45);

            builder.Property(fal => fal.UserAgent)
                .HasMaxLength(500);

            builder.Property(fal => fal.Timestamp)
                .IsRequired();

            builder.Property(fal => fal.OldValues)
                .HasColumnType("nvarchar(max)");

            builder.Property(fal => fal.NewValues)
                .HasColumnType("nvarchar(max)");

            builder.Property(fal => fal.Changes)
                .HasColumnType("nvarchar(max)");

            builder.Property(fal => fal.Details)
                .HasColumnType("nvarchar(max)");

            builder.Property(fal => fal.Reason)
                .HasMaxLength(500);

            builder.Property(fal => fal.SessionId)
                .HasMaxLength(100);

            builder.Property(fal => fal.CorrelationId)
                .HasMaxLength(100);

            builder.Property(fal => fal.Severity)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Information");

            builder.Property(fal => fal.Category)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("General");

            builder.Property(fal => fal.Metadata)
                .HasColumnType("nvarchar(max)");

            builder.Property(fal => fal.CreatedBy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fal => fal.CreatedOn)
                .IsRequired();

            builder.Property(fal => fal.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(fal => fal.LastModifiedOn);

            builder.Property(fal => fal.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(fal => fal.RowVersion)
                .IsRowVersion();

            // Indexes
            builder.HasIndex(fal => fal.FormId)
                .HasDatabaseName("IX_FormAuditLogs_FormId");

            builder.HasIndex(fal => fal.FormSubmissionId)
                .HasDatabaseName("IX_FormAuditLogs_FormSubmissionId");

            builder.HasIndex(fal => fal.TenantId)
                .HasDatabaseName("IX_FormAuditLogs_TenantId");

            builder.HasIndex(fal => fal.Action)
                .HasDatabaseName("IX_FormAuditLogs_Action");

            builder.HasIndex(fal => fal.EntityType)
                .HasDatabaseName("IX_FormAuditLogs_EntityType");

            builder.HasIndex(fal => fal.EntityId)
                .HasDatabaseName("IX_FormAuditLogs_EntityId");

            builder.HasIndex(fal => fal.UserId)
                .HasDatabaseName("IX_FormAuditLogs_UserId");

            builder.HasIndex(fal => fal.Timestamp)
                .HasDatabaseName("IX_FormAuditLogs_Timestamp");

            builder.HasIndex(fal => fal.Severity)
                .HasDatabaseName("IX_FormAuditLogs_Severity");

            builder.HasIndex(fal => fal.Category)
                .HasDatabaseName("IX_FormAuditLogs_Category");

            builder.HasIndex(fal => fal.SessionId)
                .HasDatabaseName("IX_FormAuditLogs_SessionId");

            builder.HasIndex(fal => fal.CorrelationId)
                .HasDatabaseName("IX_FormAuditLogs_CorrelationId");

            builder.HasIndex(fal => new { fal.TenantId, fal.Timestamp })
                .HasDatabaseName("IX_FormAuditLogs_TenantId_Timestamp");

            builder.HasIndex(fal => new { fal.EntityType, fal.EntityId, fal.Timestamp })
                .HasDatabaseName("IX_FormAuditLogs_EntityType_EntityId_Timestamp");

            builder.HasIndex(fal => new { fal.UserId, fal.Action, fal.Timestamp })
                .HasDatabaseName("IX_FormAuditLogs_UserId_Action_Timestamp");

            // Relationships
            builder.HasOne(fal => fal.Form)
                .WithMany(f => f.AuditLogs)
                .HasForeignKey(fal => fal.FormId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(fal => fal.FormSubmission)
                .WithMany(fs => fs.AuditLogs)
                .HasForeignKey(fal => fal.FormSubmissionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ignore domain events
            builder.Ignore(fal => fal.DomainEvents);
        }
    }
}