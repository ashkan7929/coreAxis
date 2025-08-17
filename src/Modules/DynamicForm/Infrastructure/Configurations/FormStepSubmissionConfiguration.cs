using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the FormStepSubmission entity.
    /// </summary>
    public class FormStepSubmissionConfiguration : IEntityTypeConfiguration<FormStepSubmission>
    {
        public void Configure(EntityTypeBuilder<FormStepSubmission> builder)
        {
            // Table configuration
            builder.ToTable("FormStepSubmissions");

            // Primary key
            builder.HasKey(fss => fss.Id);

            // Properties
            builder.Property(fss => fss.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(fss => fss.FormSubmissionId)
                .IsRequired();

            builder.Property(fss => fss.FormStepId)
                .IsRequired();

            builder.Property(fss => fss.StepNumber)
                .IsRequired();

            builder.Property(fss => fss.UserId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fss => fss.TenantId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fss => fss.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue(StepSubmissionStatus.NotStarted);

            builder.Property(fss => fss.StepData)
                .HasColumnType("nvarchar(max)");

            builder.Property(fss => fss.ValidationErrors)
                .HasColumnType("nvarchar(max)");

            builder.Property(fss => fss.StartedAt)
                .IsRequired(false);

            builder.Property(fss => fss.CompletedAt)
                .IsRequired(false);









            builder.Property(fss => fss.TimeSpentSeconds)
                .IsRequired()
                .HasDefaultValue(0);





            builder.Property(fss => fss.Metadata)
                .HasColumnType("nvarchar(max)");

            builder.Property(fss => fss.CreatedBy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fss => fss.CreatedOn)
                .IsRequired();

            builder.Property(fss => fss.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(fss => fss.LastModifiedOn);

            builder.Property(fss => fss.IsActive)
                .IsRequired()
                .HasDefaultValue(true);



            // Indexes
            builder.HasIndex(fss => fss.FormSubmissionId)
                .HasDatabaseName("IX_FormStepSubmissions_FormSubmissionId");

            builder.HasIndex(fss => fss.FormStepId)
                .HasDatabaseName("IX_FormStepSubmissions_FormStepId");

            builder.HasIndex(fss => fss.TenantId)
                .HasDatabaseName("IX_FormStepSubmissions_TenantId");

            builder.HasIndex(fss => fss.UserId)
                .HasDatabaseName("IX_FormStepSubmissions_UserId");

            builder.HasIndex(fss => fss.Status)
                .HasDatabaseName("IX_FormStepSubmissions_Status");

            builder.HasIndex(fss => fss.StepNumber)
                .HasDatabaseName("IX_FormStepSubmissions_StepNumber");

            builder.HasIndex(fss => fss.StartedAt)
                .HasDatabaseName("IX_FormStepSubmissions_StartedAt");

            builder.HasIndex(fss => fss.CompletedAt)
                .HasDatabaseName("IX_FormStepSubmissions_CompletedAt");

            builder.HasIndex(fss => fss.CreatedOn)
                .HasDatabaseName("IX_FormStepSubmissions_CreatedOn");

            // Composite indexes for performance
            builder.HasIndex(fss => new { fss.FormSubmissionId, fss.StepNumber })
                .IsUnique()
                .HasDatabaseName("IX_FormStepSubmissions_FormSubmissionId_StepNumber_Unique");

            builder.HasIndex(fss => new { fss.TenantId, fss.FormSubmissionId, fss.Status })
                .HasDatabaseName("IX_FormStepSubmissions_TenantId_FormSubmissionId_Status");

            builder.HasIndex(fss => new { fss.TenantId, fss.UserId, fss.Status })
                .HasDatabaseName("IX_FormStepSubmissions_TenantId_UserId_Status");

            builder.HasIndex(fss => new { fss.FormSubmissionId, fss.Status, fss.StepNumber })
                .HasDatabaseName("IX_FormStepSubmissions_FormSubmissionId_Status_StepNumber");

            // Relationships
            builder.HasOne(fss => fss.FormSubmission)
                .WithMany(fs => fs.StepSubmissions)
                .HasForeignKey(fss => fss.FormSubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(fss => fss.FormStep)
                .WithMany(fs => fs.Submissions)
                .HasForeignKey(fss => fss.FormStepId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore domain events
            builder.Ignore(fss => fss.DomainEvents);
        }
    }
}