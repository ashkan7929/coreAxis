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

            builder.Property(om => om.OccurredOn)
                .IsRequired();

            builder.Property(om => om.ProcessedOn)
                .IsRequired(false);

            builder.Property(om => om.Error)
                .HasColumnType("nvarchar(max)");

            builder.Property(om => om.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(om => om.NextRetryAt)
                .IsRequired(false);

            builder.Property(om => om.MaxRetries)
                .IsRequired()
                .HasDefaultValue(3);

            builder.Property(om => om.CorrelationId)
                .HasMaxLength(100);



            // Source and Metadata properties removed as they don't exist in OutboxMessage

            // Indexes
            builder.HasIndex(om => om.Type)
                .HasDatabaseName("IX_OutboxMessages_Type");

            builder.HasIndex(om => om.OccurredOn)
                .HasDatabaseName("IX_OutboxMessages_OccurredOn");

            builder.HasIndex(om => om.ProcessedOn)
                .HasDatabaseName("IX_OutboxMessages_ProcessedOn");

            builder.HasIndex(om => om.NextRetryAt)
                .HasDatabaseName("IX_OutboxMessages_NextRetryAt");





            // UserId and Source indexes removed as these properties don't exist in OutboxMessage

            builder.HasIndex(om => new { om.ProcessedOn, om.NextRetryAt })
                .HasDatabaseName("IX_OutboxMessages_ProcessedOn_NextRetryAt")
                .HasFilter("[ProcessedOn] IS NULL");

            builder.HasIndex(om => new { om.TenantId, om.OccurredOn })
                .HasDatabaseName("IX_OutboxMessages_TenantId_OccurredOn");
        }
    }
}