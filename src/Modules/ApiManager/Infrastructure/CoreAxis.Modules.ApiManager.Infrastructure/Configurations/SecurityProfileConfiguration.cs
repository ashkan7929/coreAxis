using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Configurations;

public class SecurityProfileConfiguration : IEntityTypeConfiguration<SecurityProfile>
{
    public void Configure(EntityTypeBuilder<SecurityProfile> builder)
    {
        builder.ToTable("SecurityProfiles", "ApiManager");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<int>();
            
        builder.Property(x => x.ConfigJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)");
            
        builder.Property(x => x.RotationPolicy)
            .HasMaxLength(500);
            
        builder.Property(x => x.CreatedAt)
            .IsRequired();
            
        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // Relationships
        builder.HasMany(x => x.WebServices)
            .WithOne(x => x.SecurityProfile)
            .HasForeignKey(x => x.SecurityProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(x => x.Type);
    }
}