using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the FormAccessPolicy entity.
    /// </summary>
    public class FormAccessPolicyConfiguration : IEntityTypeConfiguration<FormAccessPolicy>
    {
        public void Configure(EntityTypeBuilder<FormAccessPolicy> builder)
        {
            // Table configuration
            builder.ToTable("FormAccessPolicies");

            // Primary key
            builder.HasKey(fap => fap.Id);

            // Properties
            builder.Property(fap => fap.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(fap => fap.FormId)
                .IsRequired();

            builder.Property(fap => fap.TenantId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fap => fap.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(fap => fap.Description)
                .HasMaxLength(1000);

            builder.Property(fap => fap.PolicyType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(fap => fap.TargetId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fap => fap.TargetName)
                .HasMaxLength(200);

            builder.Property(fap => fap.Permissions)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(fap => fap.Conditions)
                .HasColumnType("nvarchar(max)");

            builder.Property(fap => fap.Priority)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(fap => fap.IsEnabled)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(fap => fap.EffectiveFrom)
                .IsRequired(false);

            builder.Property(fap => fap.EffectiveTo)
                .IsRequired(false);

            builder.Property(fap => fap.Metadata)
                .HasColumnType("nvarchar(max)");

            builder.Property(fap => fap.CreatedBy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fap => fap.CreatedOn)
                .IsRequired();

            builder.Property(fap => fap.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(fap => fap.LastModifiedOn);

            builder.Property(fap => fap.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(fap => fap.RowVersion)
                .IsRowVersion();

            // Indexes
            builder.HasIndex(fap => fap.FormId)
                .HasDatabaseName("IX_FormAccessPolicies_FormId");

            builder.HasIndex(fap => fap.TenantId)
                .HasDatabaseName("IX_FormAccessPolicies_TenantId");

            builder.HasIndex(fap => fap.PolicyType)
                .HasDatabaseName("IX_FormAccessPolicies_PolicyType");

            builder.HasIndex(fap => fap.TargetId)
                .HasDatabaseName("IX_FormAccessPolicies_TargetId");

            builder.HasIndex(fap => fap.Priority)
                .HasDatabaseName("IX_FormAccessPolicies_Priority");

            builder.HasIndex(fap => fap.IsEnabled)
                .HasDatabaseName("IX_FormAccessPolicies_IsEnabled");

            builder.HasIndex(fap => fap.EffectiveFrom)
                .HasDatabaseName("IX_FormAccessPolicies_EffectiveFrom");

            builder.HasIndex(fap => fap.EffectiveTo)
                .HasDatabaseName("IX_FormAccessPolicies_EffectiveTo");

            builder.HasIndex(fap => new { fap.FormId, fap.PolicyType, fap.TargetId })
                .HasDatabaseName("IX_FormAccessPolicies_FormId_PolicyType_TargetId");

            builder.HasIndex(fap => new { fap.TenantId, fap.FormId, fap.Priority })
                .HasDatabaseName("IX_FormAccessPolicies_TenantId_FormId_Priority");

            // Relationships
            builder.HasOne(fap => fap.Form)
                .WithMany(f => f.AccessPolicies)
                .HasForeignKey(fap => fap.FormId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore domain events
            builder.Ignore(fap => fap.DomainEvents);
        }
    }
}