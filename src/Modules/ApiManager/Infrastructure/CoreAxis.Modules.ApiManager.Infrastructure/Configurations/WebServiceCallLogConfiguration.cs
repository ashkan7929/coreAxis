using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Configurations;

public class WebServiceCallLogConfiguration : IEntityTypeConfiguration<WebServiceCallLog>
{
    public void Configure(EntityTypeBuilder<WebServiceCallLog> builder)
    {
        builder.ToTable("WebServiceCallLogs", "ApiManager");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);
            
        builder.Property(x => x.RequestDump)
            .HasColumnType("nvarchar(max)");
            
        builder.Property(x => x.ResponseDump)
            .HasColumnType("nvarchar(max)");
            
        builder.Property(x => x.StatusCode);
            
        builder.Property(x => x.LatencyMs)
            .IsRequired();
            
        builder.Property(x => x.Succeeded)
            .IsRequired();
            
        builder.Property(x => x.Error)
            .HasColumnType("nvarchar(max)");
            
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.WebService)
            .WithMany(x => x.CallLogs)
            .HasForeignKey(x => x.WebServiceId)
            .OnDelete(DeleteBehavior.NoAction);
            
        builder.HasOne(x => x.Method)
            .WithMany(x => x.CallLogs)
            .HasForeignKey(x => x.MethodId)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes as specified in the requirements
        builder.HasIndex(x => new { x.MethodId, x.CreatedAt });
        builder.HasIndex(x => new { x.Succeeded, x.CreatedAt });
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => x.WebServiceId);
    }
}