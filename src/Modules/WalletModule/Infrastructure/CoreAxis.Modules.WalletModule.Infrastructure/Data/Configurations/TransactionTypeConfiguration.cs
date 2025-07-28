using CoreAxis.Modules.WalletModule.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.WalletModule.Infrastructure.Data.Configurations;

public class TransactionTypeConfiguration : IEntityTypeConfiguration<TransactionType>
{
    public void Configure(EntityTypeBuilder<TransactionType> builder)
    {
        builder.ToTable("TransactionTypes");

        builder.HasKey(tt => tt.Id);

        builder.Property(tt => tt.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(tt => tt.Description)
            .HasMaxLength(500);

        builder.Property(tt => tt.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(tt => tt.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasMany(tt => tt.Transactions)
            .WithOne(t => t.TransactionType)
            .HasForeignKey(t => t.TransactionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(tt => tt.Name)
            .IsUnique()
            .HasDatabaseName("IX_TransactionTypes_Name");

        builder.HasIndex(tt => tt.Code)
            .IsUnique()
            .HasDatabaseName("IX_TransactionTypes_Code");
    }
}