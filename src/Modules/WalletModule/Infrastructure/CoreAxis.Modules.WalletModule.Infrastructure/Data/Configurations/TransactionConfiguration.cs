using CoreAxis.Modules.WalletModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Data.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.WalletId)
            .IsRequired();

        builder.Property(t => t.TransactionTypeId)
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(t => t.BalanceAfter)
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.Reference)
            .HasMaxLength(100);

        builder.Property(t => t.IdempotencyKey)
            .HasMaxLength(64);

        builder.Property(t => t.CorrelationId);

        builder.Property(t => t.Metadata)
            .HasColumnType("nvarchar(max)");

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.ProcessedAt);

        builder.Property(t => t.RelatedTransactionId);

        // Relationships
        builder.HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.TransactionType)
            .WithMany(tt => tt.Transactions)
            .HasForeignKey(t => t.TransactionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.RelatedTransaction)
            .WithMany()
            .HasForeignKey(t => t.RelatedTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(t => t.WalletId)
            .HasDatabaseName("IX_Transactions_WalletId");

        builder.HasIndex(t => t.TransactionTypeId)
            .HasDatabaseName("IX_Transactions_TransactionTypeId");

        builder.HasIndex(t => t.Reference)
            .HasDatabaseName("IX_Transactions_Reference");

        builder.HasIndex(t => t.CreatedOn)
            .HasDatabaseName("IX_Transactions_CreatedOn");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("IX_Transactions_Status");

        builder.HasIndex(t => t.RelatedTransactionId)
            .HasDatabaseName("IX_Transactions_RelatedTransactionId");

        builder.HasIndex(t => t.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_Transactions_IdempotencyKey")
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.HasIndex(t => t.CorrelationId)
            .HasDatabaseName("IX_Transactions_CorrelationId");
    }
}