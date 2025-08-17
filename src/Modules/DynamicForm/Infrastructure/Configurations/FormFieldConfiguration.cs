using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the FormField entity.
    /// </summary>
    public class FormFieldConfiguration : IEntityTypeConfiguration<FormField>
    {
        public void Configure(EntityTypeBuilder<FormField> builder)
        {
            // Table configuration
            builder.ToTable("FormFields");

            // Primary key
            builder.HasKey(ff => ff.Id);

            // Properties
            builder.Property(ff => ff.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(ff => ff.FormId)
                .IsRequired();

            builder.Property(ff => ff.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(ff => ff.Label)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(ff => ff.Description)
                .HasMaxLength(1000);

            builder.Property(ff => ff.FieldType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(ff => ff.DataType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(ff => ff.IsRequired)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(ff => ff.IsReadOnly)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(ff => ff.IsVisible)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(ff => ff.Order)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(ff => ff.DefaultValue)
                .HasMaxLength(1000);

            builder.Property(ff => ff.PlaceholderText)
                .HasMaxLength(200);

            builder.Property(ff => ff.HelpText)
                .HasMaxLength(500);

            builder.Property(ff => ff.ValidationRules)
                .HasColumnType("nvarchar(max)");

            builder.Property(ff => ff.Options)
                .HasColumnType("nvarchar(max)");

            builder.Property(ff => ff.ConditionalLogic)
                .HasColumnType("nvarchar(max)");

            builder.Property(ff => ff.CalculationFormula)
                .HasColumnType("nvarchar(max)");

            builder.Property(ff => ff.DependsOn)
                .HasMaxLength(1000);

            builder.Property(ff => ff.CssClass)
                .HasMaxLength(200);

            builder.Property(ff => ff.Style)
                .HasColumnType("nvarchar(max)");

            builder.Property(ff => ff.Attributes)
                .HasColumnType("nvarchar(max)");

            builder.Property(ff => ff.Metadata)
                .HasColumnType("nvarchar(max)");

            builder.Property(ff => ff.CreatedBy)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(ff => ff.CreatedOn)
                .IsRequired();

            builder.Property(ff => ff.LastModifiedBy)
                .HasMaxLength(100);

            builder.Property(ff => ff.LastModifiedOn);

            builder.Property(ff => ff.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(ff => ff.RowVersion)
                .IsRowVersion();

            // Indexes
            builder.HasIndex(ff => ff.FormId)
                .HasDatabaseName("IX_FormFields_FormId");

            builder.HasIndex(ff => ff.Name)
                .HasDatabaseName("IX_FormFields_Name");

            builder.HasIndex(ff => ff.FieldType)
                .HasDatabaseName("IX_FormFields_FieldType");

            builder.HasIndex(ff => ff.Order)
                .HasDatabaseName("IX_FormFields_Order");

            builder.HasIndex(ff => new { ff.FormId, ff.Name })
                .IsUnique()
                .HasDatabaseName("IX_FormFields_FormId_Name_Unique");

            builder.HasIndex(ff => new { ff.FormId, ff.Order })
                .HasDatabaseName("IX_FormFields_FormId_Order");

            // Relationships
            builder.HasOne(ff => ff.Form)
                .WithMany(f => f.Fields)
                .HasForeignKey(ff => ff.FormId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore domain events
            builder.Ignore(ff => ff.DomainEvents);
        }
    }
}