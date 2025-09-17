using CoreAxis.Modules.CommerceModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Data.Configurations;

public class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds", "commerce");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.PaymentId)
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

        builder.Property(x => x.Reason)
            .IsRequired()
            .HasMaxLength(500);

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

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(x => x.PaymentId)
            .HasDatabaseName("IX_Refunds_PaymentId");

        builder.HasIndex(x => x.TransactionId)
            .IsUnique()
            .HasDatabaseName("IX_Refunds_TransactionId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Refunds_Status");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_Refunds_CreatedAt");

        builder.HasIndex(x => x.GatewayTransactionId)
            .HasDatabaseName("IX_Refunds_GatewayTransactionId");

        // Relationships
        builder.HasOne(x => x.Payment)
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}