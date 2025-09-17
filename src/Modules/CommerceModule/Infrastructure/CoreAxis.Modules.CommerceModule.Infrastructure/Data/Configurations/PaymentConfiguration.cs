using CoreAxis.Modules.CommerceModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", "commerce");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.TransactionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PaymentMethod)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.PaymentProvider)
            .IsRequired()
            .HasMaxLength(50);

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
        builder.HasIndex(x => x.OrderId)
            .HasDatabaseName("IX_Payments_OrderId");

        builder.HasIndex(x => x.TransactionId)
            .IsUnique()
            .HasDatabaseName("IX_Payments_TransactionId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Payments_Status");

        builder.HasIndex(x => x.PaymentMethod)
            .HasDatabaseName("IX_Payments_PaymentMethod");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_Payments_CreatedAt");

        builder.HasIndex(x => x.GatewayTransactionId)
            .HasDatabaseName("IX_Payments_GatewayTransactionId");

        // Relationships
        builder.HasOne(x => x.Order)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Refunds)
            .WithOne(x => x.Payment)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}