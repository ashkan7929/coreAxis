using CoreAxis.Modules.ProductBuilderModule.Domain.Entities;
using CoreAxis.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace CoreAxis.Modules.ProductBuilderModule.Infrastructure.Data;

public class ProductBuilderDbContext : DbContext, IUnitOfWork
{
    public ProductBuilderDbContext(DbContextOptions<ProductBuilderDbContext> options) : base(options)
    {
    }

    public DbSet<ProductDefinition> ProductDefinitions { get; set; } = default!;
    public DbSet<ProductVersion> ProductVersions { get; set; } = default!;
    public DbSet<ProductBinding> ProductBindings { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductBuilderDbContext).Assembly);
        
        modelBuilder.Ignore<CoreAxis.SharedKernel.DomainEvents.DomainEvent>();

        modelBuilder.Entity<ProductVersion>()
            .HasOne(v => v.Product)
            .WithMany()
            .HasForeignKey(v => v.ProductId);

        modelBuilder.Entity<ProductBinding>()
            .HasOne(b => b.ProductVersion)
            .WithOne(v => v.Binding)
            .HasForeignKey<ProductBinding>(b => b.ProductVersionId);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await base.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        if (Database.CurrentTransaction != null) return;
        await Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (Database.CurrentTransaction == null) return;
        try
        {
            await SaveChangesAsync();
            await Database.CurrentTransaction.CommitAsync();
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (Database.CurrentTransaction == null) return;
        try
        {
            await Database.CurrentTransaction.RollbackAsync();
        }
        finally
        {
            if (Database.CurrentTransaction != null)
            {
                await Database.CurrentTransaction.DisposeAsync();
            }
        }
    }
}
