using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the FormVersion entity.
    /// </summary>
    public class FormVersionConfiguration : IEntityTypeConfiguration<FormVersion>
    {
        public void Configure(EntityTypeBuilder<FormVersion> builder)
        {
            // Table configuration
            builder.ToTable("FormVersions");

            // Primary key
            builder.HasKey(fv => fv.Id);

            // Properties
            builder.Property(fv => fv.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(fv => fv.FormId)
                .IsRequired();

            builder.Property(fv => fv.TenantId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fv => fv.Version)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(fv => fv.Schema)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(fv => fv.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(fv => fv.IsCurrent)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(fv => fv.PublishedAt)
                .IsRequired(false);

            builder.Property(fv => fv.PublishedBy)
                .HasMaxLength(100);

            builder.Property(fv => fv.ChangeLog)
                .HasColumnType("nvarchar(max)");

            builder.Property(fv => fv.Metadata)
                .HasColumnType("nvarchar(max)");

            builder.Property(fv => fv.CreatedBy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(fv => fv.CreatedOn)
                .IsRequired();

            builder.Property(fv => fv.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(fv => fv.LastModifiedOn);

            builder.Property(fv => fv.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(fv => fv.RowVersion)
                .IsRowVersion();

            // Indexes
            builder.HasIndex(fv => fv.FormId)
                .HasDatabaseName("IX_FormVersions_FormId");

            builder.HasIndex(fv => fv.TenantId)
                .HasDatabaseName("IX_FormVersions_TenantId");

            builder.HasIndex(fv => fv.Version)
                .HasDatabaseName("IX_FormVersions_Version");

            builder.HasIndex(fv => fv.IsPublished)
                .HasDatabaseName("IX_FormVersions_IsPublished");

            builder.HasIndex(fv => fv.IsCurrent)
                .HasDatabaseName("IX_FormVersions_IsCurrent");

            builder.HasIndex(fv => fv.PublishedAt)
                .HasDatabaseName("IX_FormVersions_PublishedAt");

            builder.HasIndex(fv => new { fv.FormId, fv.Version })
                .IsUnique()
                .HasDatabaseName("IX_FormVersions_FormId_Version_Unique");

            builder.HasIndex(fv => new { fv.FormId, fv.IsCurrent })
                .HasDatabaseName("IX_FormVersions_FormId_IsCurrent")
                .HasFilter("[IsCurrent] = 1");

            // Relationships
            builder.HasOne(fv => fv.Form)
                .WithMany(f => f.Versions)
                .HasForeignKey(fv => fv.FormId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore domain events
            builder.Ignore(fv => fv.DomainEvents);
        }
    }
}