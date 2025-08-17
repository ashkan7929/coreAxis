using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the FormSubmission entity.
    /// </summary>
    public class FormSubmissionConfiguration : IEntityTypeConfiguration<FormSubmission>
    {
        public void Configure(EntityTypeBuilder<FormSubmission> builder)
        {
            // Table configuration
            builder.ToTable("FormSubmissions");

            // Primary key
            builder.HasKey(fs => fs.Id);

            // Properties
            builder.Property(fs => fs.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(fs => fs.FormId)
                .IsRequired();

            builder.Property(fs => fs.TenantId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fs => fs.SubmissionData)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(fs => fs.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Draft");

            builder.Property(fs => fs.SubmittedBy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fs => fs.SubmittedAt)
                .IsRequired();

            builder.Property(fs => fs.ProcessedAt)
                .IsRequired(false);

            builder.Property(fs => fs.ProcessedBy)
                .HasMaxLength(100);

            builder.Property(fs => fs.ValidationErrors)
                .HasColumnType("nvarchar(max)");

            builder.Property(fs => fs.ProcessingErrors)
                .HasColumnType("nvarchar(max)");

            builder.Property(fs => fs.ProcessingResult)
                .HasColumnType("nvarchar(max)");

            builder.Property(fs => fs.WorkflowInstanceId)
                .HasMaxLength(100);

            builder.Property(fs => fs.WorkflowStatus)
                .HasMaxLength(50);

            builder.Property(fs => fs.Priority)
                .IsRequired()
                .HasDefaultValue("Normal");

            builder.Property(fs => fs.Source)
                .HasMaxLength(100);

            builder.Property(fs => fs.IpAddress)
                .HasMaxLength(45);

            builder.Property(fs => fs.UserAgent)
                .HasMaxLength(500);

            builder.Property(fs => fs.SessionId)
                .HasMaxLength(100);

            builder.Property(fs => fs.CorrelationId)
                .HasMaxLength(100);

            builder.Property(fs => fs.ParentSubmissionId)
                .IsRequired(false);

            builder.Property(fs => fs.ReferenceNumber)
                .HasMaxLength(50);

            builder.Property(fs => fs.Tags)
                .HasMaxLength(500);

            builder.Property(fs => fs.Metadata)
                .HasColumnType("nvarchar(max)");

            builder.Property(fs => fs.CreatedBy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fs => fs.CreatedOn)
                .IsRequired();

            builder.Property(fs => fs.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(fs => fs.LastModifiedOn);

            builder.Property(fs => fs.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(fs => fs.RowVersion)
                .IsRowVersion();

            // Indexes
            builder.HasIndex(fs => fs.FormId)
                .HasDatabaseName("IX_FormSubmissions_FormId");

            builder.HasIndex(fs => fs.TenantId)
                .HasDatabaseName("IX_FormSubmissions_TenantId");

            builder.HasIndex(fs => fs.Status)
                .HasDatabaseName("IX_FormSubmissions_Status");

            builder.HasIndex(fs => fs.SubmittedBy)
                .HasDatabaseName("IX_FormSubmissions_SubmittedBy");

            builder.HasIndex(fs => fs.SubmittedAt)
                .HasDatabaseName("IX_FormSubmissions_SubmittedAt");

            builder.HasIndex(fs => fs.WorkflowInstanceId)
                .HasDatabaseName("IX_FormSubmissions_WorkflowInstanceId");

            builder.HasIndex(fs => fs.ReferenceNumber)
                .IsUnique()
                .HasDatabaseName("IX_FormSubmissions_ReferenceNumber_Unique")
                .HasFilter("[ReferenceNumber] IS NOT NULL");

            builder.HasIndex(fs => fs.ParentSubmissionId)
                .HasDatabaseName("IX_FormSubmissions_ParentSubmissionId");

            builder.HasIndex(fs => new { fs.TenantId, fs.FormId, fs.SubmittedAt })
                .HasDatabaseName("IX_FormSubmissions_TenantId_FormId_SubmittedAt");

            // Relationships
            builder.HasOne(fs => fs.Form)
                .WithMany(f => f.Submissions)
                .HasForeignKey(fs => fs.FormId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(fs => fs.ParentSubmission)
                .WithMany(fs => fs.ChildSubmissions)
                .HasForeignKey(fs => fs.ParentSubmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(fs => fs.AuditLogs)
                .WithOne(fal => fal.FormSubmission)
                .HasForeignKey(fal => fal.FormSubmissionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ignore domain events
            builder.Ignore(fs => fs.DomainEvents);
        }
    }
}