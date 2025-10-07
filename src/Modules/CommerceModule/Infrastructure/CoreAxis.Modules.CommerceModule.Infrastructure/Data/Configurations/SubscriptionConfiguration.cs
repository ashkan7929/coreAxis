using CoreAxis.Modules.CommerceModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Data.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions", "commerce");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.PlanName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.BillingCycle)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate);

        builder.Property(x => x.NextBillingDate);

        builder.Property(x => x.LastBillingDate);

        builder.Property(x => x.TrialEndDate);

        builder.Property(x => x.CancelledAt);

        builder.Property(x => x.AutoRenew)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_Subscriptions_UserId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Subscriptions_Status");

        builder.HasIndex(x => x.PlanName)
            .HasDatabaseName("IX_Subscriptions_PlanName");

        builder.HasIndex(x => x.NextBillingDate)
            .HasDatabaseName("IX_Subscriptions_NextBillingDate");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_Subscriptions_CreatedAt");

        builder.HasIndex(x => new { x.UserId, x.PlanName, x.Status })
            .HasDatabaseName("IX_Subscriptions_UserPlanStatus");

        // Relationships
        builder.HasMany(x => x.Payments)
            .WithOne(x => x.Subscription)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}