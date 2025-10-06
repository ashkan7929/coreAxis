using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Configurations;

public class WebServiceMethodConfiguration : IEntityTypeConfiguration<WebServiceMethod>
{
    public void Configure(EntityTypeBuilder<WebServiceMethod> builder)
    {
        builder.ToTable("WebServiceMethods", "ApiManager");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Path)
            .IsRequired()
            .HasMaxLength(500);
            
        builder.Property(x => x.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);
            
        builder.Property(x => x.RequestSchema)
            .HasColumnType("nvarchar(max)");
            
        builder.Property(x => x.ResponseSchema)
            .HasColumnType("nvarchar(max)");
            
        builder.Property(x => x.TimeoutMs)
            .IsRequired();
            
        builder.Property(x => x.RetryPolicyJson)
            .HasColumnType("nvarchar(max)");
            
        builder.Property(x => x.CircuitPolicyJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.EndpointConfigJson)
            .HasColumnType("nvarchar(max)");
            
        builder.Property(x => x.IsActive)
            .IsRequired();
            
        builder.Property(x => x.CreatedAt)
            .IsRequired();
            
        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.WebService)
            .WithMany(x => x.Methods)
            .HasForeignKey(x => x.WebServiceId)
            .OnDelete(DeleteBehavior.NoAction);
            
        builder.HasMany(x => x.Parameters)
            .WithOne(x => x.Method)
            .HasForeignKey(x => x.MethodId)
            .OnDelete(DeleteBehavior.NoAction);
            
        builder.HasMany(x => x.CallLogs)
            .WithOne(x => x.Method)
            .HasForeignKey(x => x.MethodId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        builder.HasIndex(x => x.WebServiceId);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.WebServiceId, x.Path, x.HttpMethod })
            .IsUnique();
    }
}