using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreAxis.Modules.MLMModule.Domain.Entities;
using CoreAxis.Modules.MLMModule.Domain.Enums;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Data.Configurations;

public class CommissionTransactionConfiguration : IEntityTypeConfiguration<CommissionTransaction>
{
    public void Configure(EntityTypeBuilder<CommissionTransaction> builder)
    {
        builder.ToTable("CommissionTransactions");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.UserId)
            .IsRequired();
            
        builder.Property(x => x.SourcePaymentId)
            .IsRequired();
            
        builder.Property(x => x.CommissionRuleSetId)
            .IsRequired();
            
        builder.Property(x => x.RuleSetCode)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(x => x.RuleVersion)
            .IsRequired();
            
        builder.Property(x => x.CorrelationId)
            .IsRequired();
            
        builder.Property(x => x.Level)
            .IsRequired();
            
        builder.Property(x => x.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,6)");
            
        builder.Property(x => x.SourceAmount)
            .IsRequired()
            .HasColumnType("decimal(18,6)");
            
        builder.Property(x => x.Percentage)
            .IsRequired()
            .HasColumnType("decimal(5,4)");
            
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
            
        builder.Property(x => x.IsSettled)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.Property(x => x.Notes)
            .IsRequired(false)
            .HasMaxLength(500);
            
        builder.Property(x => x.CreatedOn)
            .IsRequired();
            
        builder.Property(x => x.LastModifiedOn)
            .IsRequired(false);
        
        // Indexes for performance
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_CommissionTransaction_UserId");
            
        builder.HasIndex(x => x.SourcePaymentId)
            .HasDatabaseName("IX_CommissionTransaction_SourcePaymentId");
            
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_CommissionTransaction_Status");
            
        builder.HasIndex(x => x.CreatedOn)
            .HasDatabaseName("IX_CommissionTransaction_CreatedOn");
            
        builder.HasIndex(x => new { x.UserId, x.Status })
            .HasDatabaseName("IX_CommissionTransaction_UserId_Status");
            
        builder.HasIndex(x => new { x.SourcePaymentId, x.UserId })
            .HasDatabaseName("IX_CommissionTransaction_SourcePaymentId_UserId");
            
        builder.HasIndex(x => new { x.Status, x.CreatedOn })
            .HasDatabaseName("IX_CommissionTransaction_Status_CreatedOn");
        
        // Relationships
        builder.HasOne(x => x.CommissionRuleSet)
            .WithMany()
            .HasForeignKey(x => x.CommissionRuleSetId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(x => x.UserReferral)
            .WithMany(x => x.EarnedCommissions)
            .HasForeignKey(x => x.UserId)
            .HasPrincipalKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}