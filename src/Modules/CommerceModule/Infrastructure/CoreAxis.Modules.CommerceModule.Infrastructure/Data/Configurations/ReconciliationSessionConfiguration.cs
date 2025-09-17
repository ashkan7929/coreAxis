using CoreAxis.Modules.CommerceModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.CommerceModule.Infrastructure.Data.Configurations;

public class ReconciliationSessionConfiguration : IEntityTypeConfiguration<ReconciliationSession>
{
    public void Configure(EntityTypeBuilder<ReconciliationSession> builder)
    {
        builder.ToTable("ReconciliationSessions", "commerce");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.SessionName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate)
            .IsRequired();

        builder.Property(x => x.TotalTransactions)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ReconciledTransactions)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.UnreconciledTransactions)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.ReconciledAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.UnreconciledAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.Property(x => x.CompletedAt);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ReconciliationSessions_Status");

        builder.HasIndex(x => x.StartDate)
            .HasDatabaseName("IX_ReconciliationSessions_StartDate");

        builder.HasIndex(x => x.EndDate)
            .HasDatabaseName("IX_ReconciliationSessions_EndDate");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_ReconciliationSessions_CreatedAt");

        // Relationships
        builder.HasMany(x => x.Entries)
            .WithOne(x => x.Session)
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}