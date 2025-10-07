using CoreAxis.Modules.DynamicForm.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    public class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
    {
        public void Configure(EntityTypeBuilder<DataSource> builder)
        {
            builder.ToTable("DataSources");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.ServiceName)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.EndpointName)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.CacheTtlSeconds)
                .IsRequired();

            builder.Property(x => x.Enabled)
                .IsRequired();

            builder.HasIndex(x => new { x.ServiceName, x.EndpointName })
                   .HasDatabaseName("IX_DataSource_Service_Endpoint");
        }
    }
}