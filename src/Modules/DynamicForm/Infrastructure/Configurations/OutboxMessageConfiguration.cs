using CoreAxis.SharedKernel.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.DynamicForm.Infrastructure.Configurations
{
    /// <summary>
    /// Entity Framework configuration for the OutboxMessage entity.
    /// </summary>
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            // Table configuration
            builder.ToTable("OutboxMessages");

            // Primary key
            builder.HasKey(om => om.Id);

            // Properties
            builder.Property(om => om.Id)
                .IsRequired()
                .ValueGeneratedNever();

            builder.Property(om => om.Type)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(om => om.Content)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(om => om.OccurredOnUtc)
                .IsRequired();

            builder.Property(om => om.ProcessedOnUtc)
                .IsRequired(false);

            builder.Property(om => om.Error)
                .HasColumnType("nvarchar(max)");

            builder.Property(om => om.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(om => om.NextRetryOnUtc)
                .IsRequired(false);

            builder.Property(om => om.MaxRetries)
                .IsRequired()
                .HasDefaultValue(3);

            builder.Property(om => om.Priority)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(om => om.CorrelationId)
                .HasMaxLength(100);

            builder.Property(om => om.TenantId)
                .HasMaxLength(100);

            builder.Property(om => om.UserId)
                .HasMaxLength(100);

            builder.Property(om => om.Source)
                .HasMaxLength(100);

            builder.Property(om => om.Metadata)
                .HasColumnType("nvarchar(max)");

            // Indexes
            builder.HasIndex(om => om.Type)
                .HasDatabaseName("IX_OutboxMessages_Type");

            builder.HasIndex(om => om.OccurredOnUtc)
                .HasDatabaseName("IX_OutboxMessages_OccurredOnUtc");

            builder.HasIndex(om => om.ProcessedOnUtc)
                .HasDatabaseName("IX_OutboxMessages_ProcessedOnUtc");

            builder.HasIndex(om => om.NextRetryOnUtc)
                .HasDatabaseName("IX_OutboxMessages_NextRetryOnUtc");

            builder.HasIndex(om => om.Priority)
                .HasDatabaseName("IX_OutboxMessages_Priority");

            builder.HasIndex(om => om.CorrelationId)
                .HasDatabaseName("IX_OutboxMessages_CorrelationId");

            builder.HasIndex(om => om.TenantId)
                .HasDatabaseName("IX_OutboxMessages_TenantId");

            builder.HasIndex(om => om.UserId)
                .HasDatabaseName("IX_OutboxMessages_UserId");

            builder.HasIndex(om => om.Source)
                .HasDatabaseName("IX_OutboxMessages_Source");

            builder.HasIndex(om => new { om.ProcessedOnUtc, om.NextRetryOnUtc, om.Priority })
                .HasDatabaseName("IX_OutboxMessages_ProcessedOnUtc_NextRetryOnUtc_Priority")
                .HasFilter("[ProcessedOnUtc] IS NULL");

            builder.HasIndex(om => new { om.TenantId, om.OccurredOnUtc })
                .HasDatabaseName("IX_OutboxMessages_TenantId_OccurredOnUtc");
        }
    }
}