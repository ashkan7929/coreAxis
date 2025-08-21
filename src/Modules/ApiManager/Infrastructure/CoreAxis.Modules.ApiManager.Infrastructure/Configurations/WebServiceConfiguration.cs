using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Configurations;

public class WebServiceConfiguration : IEntityTypeConfiguration<WebService>
{
    public void Configure(EntityTypeBuilder<WebService> builder)
    {
        builder.ToTable("WebServices", "ApiManager");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(x => x.BaseUrl)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(x => x.Description)
            .HasMaxLength(1000);
            
        builder.Property(x => x.OwnerTenantId)
            .HasMaxLength(100);
            
        builder.Property(x => x.CreatedAt)
            .IsRequired();
            
        builder.Property(x => x.UpdatedAt)
            .IsRequired();
            
        builder.Property(x => x.IsActive)
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.SecurityProfile)
            .WithMany(x => x.WebServices)
            .HasForeignKey(x => x.SecurityProfileId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasMany(x => x.Methods)
            .WithOne(x => x.WebService)
            .HasForeignKey(x => x.WebServiceId)
            .OnDelete(DeleteBehavior.NoAction);
            
        builder.HasMany(x => x.CallLogs)
            .WithOne(x => x.WebService)
            .HasForeignKey(x => x.WebServiceId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.OwnerTenantId);
    }
}