using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the Form entity.
    /// </summary>
    public class FormConfiguration : IEntityTypeConfiguration<Form>
    {
        public void Configure(EntityTypeBuilder<Form> builder)
        {
            // Table configuration
            builder.ToTable("Forms");

            // Primary key
            builder.HasKey(f => f.Id);

            // Properties
            builder.Property(f => f.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(f => f.TenantId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(f => f.Description)
                .HasMaxLength(1000);













            builder.Property(f => f.Version)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("1.0.0");

            builder.Property(f => f.IsPublished)
                .IsRequired()
                .HasDefaultValue(false);









            builder.Property(f => f.Metadata)
                .HasColumnType("nvarchar(max)");

            builder.Property(f => f.CreatedBy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(f => f.CreatedOn)
                .IsRequired();

            builder.Property(f => f.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(f => f.LastModifiedOn);

            builder.Property(f => f.IsActive)
                .IsRequired()
                .HasDefaultValue(true);



            // Indexes
            builder.HasIndex(f => f.TenantId)
                .HasDatabaseName("IX_Forms_TenantId");

            builder.HasIndex(f => f.Name)
                .HasDatabaseName("IX_Forms_Name");





            builder.HasIndex(f => f.IsPublished)
                .HasDatabaseName("IX_Forms_IsPublished");

            builder.HasIndex(f => f.CreatedOn)
                .HasDatabaseName("IX_Forms_CreatedOn");

            builder.HasIndex(f => new { f.TenantId, f.Name })
                .IsUnique()
                .HasDatabaseName("IX_Forms_TenantId_Name_Unique");

            // Relationships
            builder.HasMany(f => f.Fields)
                .WithOne(ff => ff.Form)
                .HasForeignKey(ff => ff.FormId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.Submissions)
                .WithOne(fs => fs.Form)
                .HasForeignKey(fs => fs.FormId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(f => f.Versions)
                .WithOne(fv => fv.Form)
                .HasForeignKey(fv => fv.FormId)
                .OnDelete(DeleteBehavior.Cascade);





            // Ignore domain events
            builder.Ignore(f => f.DomainEvents);
        }
    }
}