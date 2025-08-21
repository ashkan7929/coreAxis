using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Configurations;

public class WebServiceParamConfiguration : IEntityTypeConfiguration<WebServiceParam>
{
    public void Configure(EntityTypeBuilder<WebServiceParam> builder)
    {
        builder.ToTable("WebServiceParams", "ApiManager");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.Location)
            .IsRequired()
            .HasConversion<int>();
            
        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(x => x.IsRequired)
            .IsRequired();
            
        builder.Property(x => x.DefaultValue)
            .HasMaxLength(500);
            
        builder.Property(x => x.CreatedAt)
            .IsRequired();
            
        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.Method)
            .WithMany(x => x.Parameters)
            .HasForeignKey(x => x.MethodId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        builder.HasIndex(x => x.MethodId);
        builder.HasIndex(x => new { x.MethodId, x.Name })
            .IsUnique();
    }
}