using CoreAxis.Modules.CommerceModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Data.Configurations;

public class CouponRedemptionConfiguration : IEntityTypeConfiguration<CouponRedemption>
{
    public void Configure(EntityTypeBuilder<CouponRedemption> builder)
    {
        builder.ToTable("CouponRedemptions", "commerce");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.CouponCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DiscountAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.RedeemedAt)
            .IsRequired();

        builder.Property(x => x.ExpiresAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_CouponRedemptions_UserId");

        builder.HasIndex(x => x.OrderId)
            .HasDatabaseName("IX_CouponRedemptions_OrderId");

        builder.HasIndex(x => x.CouponCode)
            .HasDatabaseName("IX_CouponRedemptions_CouponCode");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_CouponRedemptions_Status");

        builder.HasIndex(x => x.RedeemedAt)
            .HasDatabaseName("IX_CouponRedemptions_RedeemedAt");

        builder.HasIndex(x => new { x.CouponCode, x.UserId })
            .HasDatabaseName("IX_CouponRedemptions_CouponUser");
    }
}