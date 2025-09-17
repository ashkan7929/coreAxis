using CoreAxis.Modules.CommerceModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Data.Configurations;

public class DiscountRuleConfiguration : IEntityTypeConfiguration<DiscountRule>
{
    public void Configure(EntityTypeBuilder<DiscountRule> builder)
    {
        builder.ToTable("DiscountRules", "commerce");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(1000);

        builder.Property(x => x.DiscountType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DiscountValue)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MinimumOrderAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.MaximumDiscountAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate);

        builder.Property(x => x.UsageLimit);

        builder.Property(x => x.UsageCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_DiscountRules_Name");

        builder.HasIndex(x => x.DiscountType)
            .HasDatabaseName("IX_DiscountRules_DiscountType");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_DiscountRules_IsActive");

        builder.HasIndex(x => x.StartDate)
            .HasDatabaseName("IX_DiscountRules_StartDate");

        builder.HasIndex(x => x.EndDate)
            .HasDatabaseName("IX_DiscountRules_EndDate");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_DiscountRules_CreatedAt");

        // Relationships
        builder.HasMany(x => x.Orders)
            .WithMany(x => x.DiscountRules)
            .UsingEntity("OrderDiscountRules");
    }
}