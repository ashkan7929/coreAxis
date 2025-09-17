using CoreAxis.Modules.CommerceModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Data.Configurations;

public class ReconciliationEntryConfiguration : IEntityTypeConfiguration<ReconciliationEntry>
{
    public void Configure(EntityTypeBuilder<ReconciliationEntry> builder)
    {
        builder.ToTable("ReconciliationEntries", "commerce");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.SessionId)
            .IsRequired();

        builder.Property(x => x.TransactionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.TransactionType)
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

        builder.Property(x => x.GatewayAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Variance)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.TransactionDate)
            .IsRequired();

        builder.Property(x => x.ReconciledAt);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.ReconciledBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(x => x.SessionId)
            .HasDatabaseName("IX_ReconciliationEntries_SessionId");

        builder.HasIndex(x => x.TransactionId)
            .HasDatabaseName("IX_ReconciliationEntries_TransactionId");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ReconciliationEntries_Status");

        builder.HasIndex(x => x.TransactionType)
            .HasDatabaseName("IX_ReconciliationEntries_TransactionType");

        builder.HasIndex(x => x.TransactionDate)
            .HasDatabaseName("IX_ReconciliationEntries_TransactionDate");

        builder.HasIndex(x => x.GatewayTransactionId)
            .HasDatabaseName("IX_ReconciliationEntries_GatewayTransactionId");

        // Relationships
        builder.HasOne(x => x.Session)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}