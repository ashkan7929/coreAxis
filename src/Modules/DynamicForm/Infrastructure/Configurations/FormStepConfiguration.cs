using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the FormStep entity.
    /// </summary>
    public class FormStepConfiguration : IEntityTypeConfiguration<FormStep>
    {
        public void Configure(EntityTypeBuilder<FormStep> builder)
        {
            // Table configuration
            builder.ToTable("FormSteps");

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



            builder.Property(fs => fs.Title)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(fs => fs.Description)
                .HasMaxLength(1000);

            builder.Property(fs => fs.StepNumber)
                .IsRequired();

            builder.Property(fs => fs.StepType)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Standard");





            builder.Property(fs => fs.ValidationRules)
                .HasColumnType("nvarchar(max)");





            builder.Property(fs => fs.IsRequired)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(fs => fs.IsSkippable)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(fs => fs.IsRepeatable)
                .IsRequired()
                .HasDefaultValue(false);





            builder.Property(fs => fs.DependsOnSteps)
                .HasMaxLength(500);

            builder.Property(fs => fs.ConditionalLogic)
                .HasColumnType("nvarchar(max)");

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
                .HasDatabaseName("IX_FormSteps_FormId");

            builder.HasIndex(fs => fs.TenantId)
                .HasDatabaseName("IX_FormSteps_TenantId");

            builder.HasIndex(fs => fs.StepNumber)
                .HasDatabaseName("IX_FormSteps_StepNumber");

            builder.HasIndex(fs => fs.StepType)
                .HasDatabaseName("IX_FormSteps_StepType");



            builder.HasIndex(fs => fs.IsRequired)
                .HasDatabaseName("IX_FormSteps_IsRequired");

            builder.HasIndex(fs => fs.IsSkippable)
                .HasDatabaseName("IX_FormSteps_IsSkippable");

            builder.HasIndex(fs => fs.CreatedOn)
                .HasDatabaseName("IX_FormSteps_CreatedOn");

            // Composite indexes for performance
            builder.HasIndex(fs => new { fs.FormId, fs.StepNumber })
                .IsUnique()
                .HasDatabaseName("IX_FormSteps_FormId_StepNumber_Unique");

            builder.HasIndex(fs => new { fs.TenantId, fs.FormId, fs.IsActive })
                .HasDatabaseName("IX_FormSteps_TenantId_FormId_IsActive");

            builder.HasIndex(fs => new { fs.FormId, fs.IsActive, fs.StepNumber })
                .HasDatabaseName("IX_FormSteps_FormId_IsActive_StepNumber");

            builder.HasIndex(fs => new { fs.TenantId, fs.StepType, fs.IsActive })
                .HasDatabaseName("IX_FormSteps_TenantId_StepType_IsActive");

            // Relationships
            builder.HasOne(fs => fs.Form)
                .WithMany(f => f.Steps)
                .HasForeignKey(fs => fs.FormId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(fs => fs.Submissions)
                .WithOne(fss => fss.FormStep)
                .HasForeignKey(fss => fss.FormStepId)
                .OnDelete(DeleteBehavior.Restrict);

            // Check constraints
            builder.HasCheckConstraint("CK_FormSteps_StepNumber", "[StepNumber] > 0");
            builder.HasCheckConstraint("CK_FormSteps_MaxAttempts", "[MaxAttempts] > 0");
            builder.HasCheckConstraint("CK_FormSteps_TimeoutMinutes", "[TimeoutMinutes] IS NULL OR [TimeoutMinutes] > 0");

            // Ignore domain events
            builder.Ignore(fs => fs.DomainEvents);
        }
    }
}