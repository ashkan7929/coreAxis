using CoreAxis.Modules.CommerceModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Data.Configurations;

public class SubscriptionPaymentConfiguration : IEntityTypeConfiguration<SubscriptionPayment>
{
    public void Configure(EntityTypeBuilder<SubscriptionPayment> builder)
    {
        builder.ToTable("SubscriptionPayments", "commerce");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.SubscriptionId)
            .IsRequired();

        builder.Property(x => x.TransactionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PaymentProvider)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.BillingPeriodStart)
            .IsRequired();

        builder.Property(x => x.BillingPeriodEnd)
            .IsRequired();

        builder.Property(x => x.GatewayTransactionId)
            .HasMaxLength(200);

        builder.Property(x => x.GatewayResponse)
            .HasMaxLength(2000);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(1000);

        builder.Property(x => x.ProcessedAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.SubscriptionId)
            .HasDatabaseName("IX_SubscriptionPayments_SubscriptionId");

        builder.HasIndex(x => x.TransactionId)
            .IsUnique()
            .HasDatabaseName("IX_SubscriptionPayments_TransactionId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_SubscriptionPayments_Status");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_SubscriptionPayments_CreatedAt");

        builder.HasIndex(x => x.BillingPeriodStart)
            .HasDatabaseName("IX_SubscriptionPayments_BillingPeriodStart");

        builder.HasIndex(x => x.GatewayTransactionId)
            .HasDatabaseName("IX_SubscriptionPayments_GatewayTransactionId");

        // Relationships
        builder.HasOne(x => x.Subscription)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}