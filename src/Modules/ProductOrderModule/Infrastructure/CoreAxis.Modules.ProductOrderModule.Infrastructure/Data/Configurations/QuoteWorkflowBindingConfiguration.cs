using CoreAxis.Modules.ProductOrderModule.Domain.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Data.Configurations;

public class QuoteWorkflowBindingConfiguration : IEntityTypeConfiguration<QuoteWorkflowBinding>
{
    public void Configure(EntityTypeBuilder<QuoteWorkflowBinding> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssetCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.WorkflowCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.WorkflowVersion)
            .IsRequired();

        builder.Property(x => x.ReturnMappingSetId)
            .IsRequired();

        // Index for fast lookup by AssetCode
        builder.HasIndex(x => x.AssetCode)
            .HasDatabaseName("IX_QuoteWorkflowBindings_AssetCode");
    }
}
