# Infrastructure

## Purpose

The Infrastructure layer provides concrete implementations of interfaces defined in the Domain and Application layers. It deals with technical details such as databases, external services, security, and logging.

## Key Components

### Data Access

- **DbContext**: Database contexts
- **Repositories**: Implementations of data repositories
- **Migrations**: Database migrations

### Security

- **Authentication**: Authentication using JWT or OAuth
- **Authorization**: Authorization, roles, and permissions

### External Integration

- **HttpClients**: HTTP clients for communicating with external services
- **ExternalServices**: Implementations of external services

### Shared Infrastructure

- **Logging**: Event and error logging
- **Caching**: Caching
- **BackgroundJobs**: Background tasks and scheduling

## How to Use

### Service Registration

```csharp
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register database context
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Register repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        // Register other infrastructure services
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}
```

### Repository Implementation

```csharp
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProductRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Product> GetByIdAsync(Guid id)
    {
        return await _dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _dbContext.Products
            .Where(p => p.IsActive)
            .ToListAsync();
    }

    public async Task<Product> AddAsync(Product product)
    {
        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        _dbContext.Entry(product).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Product product)
    {
        product.IsActive = false;
        await UpdateAsync(product);
    }
}
```