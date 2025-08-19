using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CoreAxis.Modules.MLMModule.Domain.Entities;

namespace CoreAxis.Modules.MLMModule.Infrastructure.Data.Configurations;

public class UserReferralConfiguration : IEntityTypeConfiguration<UserReferral>
{
    public void Configure(EntityTypeBuilder<UserReferral> builder)
    {
        builder.ToTable("UserReferrals");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.UserId)
            .IsRequired();
            
        builder.Property(x => x.ParentUserId)
            .IsRequired(false);
            
        builder.Property(x => x.Path)
            .IsRequired()
            .HasMaxLength(4000); // Support deep hierarchies
            
        builder.Property(x => x.Level)
            .IsRequired();
            
        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        builder.Property(x => x.JoinedAt)
            .IsRequired();
            
        builder.Property(x => x.CreatedOn)
            .IsRequired();
            
        builder.Property(x => x.LastModifiedOn)
            .IsRequired(false);
        
        // Indexes for performance
        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserReferrals_UserId");
            
        builder.HasIndex(x => x.ParentUserId)
            .HasDatabaseName("IX_UserReferrals_ParentUserId");
            
        builder.HasIndex(x => x.Path)
            .HasDatabaseName("IX_UserReferrals_Path");
            
        builder.HasIndex(x => new { x.Level, x.IsActive })
            .HasDatabaseName("IX_UserReferrals_Level_IsActive");
        
        // Self-referencing relationship
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentUserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // Relationship with CommissionTransactions
        builder.HasMany(x => x.EarnedCommissions)
            .WithOne(x => x.UserReferral)
            .HasForeignKey(x => x.UserId)
            .HasPrincipalKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}