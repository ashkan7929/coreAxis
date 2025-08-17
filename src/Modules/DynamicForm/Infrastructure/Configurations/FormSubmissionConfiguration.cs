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



            builder.Property(fs => fs.Status)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("Draft");



            builder.Property(fs => fs.SubmittedAt)
                .IsRequired();





            builder.Property(fs => fs.ValidationErrors)
                .HasColumnType("nvarchar(max)");















            builder.Property(fs => fs.UserAgent)
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



            // Indexes
            builder.HasIndex(fs => fs.FormId)
                .HasDatabaseName("IX_FormSubmissions_FormId");

            builder.HasIndex(fs => fs.TenantId)
                .HasDatabaseName("IX_FormSubmissions_TenantId");

            builder.HasIndex(fs => fs.Status)
                .HasDatabaseName("IX_FormSubmissions_Status");



            builder.HasIndex(fs => fs.SubmittedAt)
                .HasDatabaseName("IX_FormSubmissions_SubmittedAt");



            // ReferenceNumber and ParentSubmissionId indexes removed as these properties don't exist

            builder.HasIndex(fs => new { fs.TenantId, fs.FormId, fs.SubmittedAt })
                .HasDatabaseName("IX_FormSubmissions_TenantId_FormId_SubmittedAt");

            // Relationships
            builder.HasOne(fs => fs.Form)
                .WithMany(f => f.Submissions)
                .HasForeignKey(fs => fs.FormId)
                .OnDelete(DeleteBehavior.Cascade);

            // ParentSubmission and AuditLogs relationships removed as these properties don't exist

            // Ignore domain events
            builder.Ignore(fs => fs.DomainEvents);
        }
    }
}