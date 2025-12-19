using CoreAxis.Modules.ProductOrderModule.Domain.Quotes;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Data.Configurations;

public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.HasKey(q => q.Id);
        
        builder.Property(q => q.AssetCode)
            .HasConversion(
                v => v.Value,
                v => AssetCode.Create(v))
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(q => q.ApplicationData).IsRequired();
        builder.Property(q => q.ResultBlocks);
        builder.Property(q => q.Status).IsRequired();
        builder.Property(q => q.ExpirationDate).IsRequired();
        builder.Property(q => q.FinalPremium).HasColumnType("decimal(18,0)");
        
        // MVP: Audit fields optional
        builder.Property(q => q.CreatedBy).IsRequired(false);
        builder.Property(q => q.LastModifiedBy).IsRequired(false);
    }
}
