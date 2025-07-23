# SharedKernel

## Purpose

The SharedKernel is a collection of patterns, abstractions, and core entities shared across all system modules. It provides a common language and consistent foundation for module development.

## Key Components

### Core Entities

- **EntityBase**: Base class for all entities with unique identifier and tracking properties
- **ValueObject**: Base class for value objects with comparison logic
- **CoreAxisException**: Core system exception

### Result Patterns

- **Result<T>**: Pattern for handling operation results in a unified way
- **PaginatedList<T>**: Paginated list with pagination information

### Localization

- **ILocalizationService**: Interface for translation and localization services
- **LocalizationService**: Implementation of translation services using .NET resources

## How to Use

### Using EntityBase

```csharp
public class Product : EntityBase
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    private Product() { } // Required for EF Core

    public static Product Create(string name, decimal price)
    {
        return new Product
        {
            Name = name,
            Price = price
        };
    }
}
```

### Using Result

```csharp
public async Task<Result<Product>> GetProductByIdAsync(Guid id)
{
    var product = await _repository.GetByIdAsync(id);
    if (product == null)
        return Result<Product>.Failure("Product not found");

    return Result<Product>.Success(product);
}
```

### Using PaginatedList

```csharp
public async Task<PaginatedList<ProductDto>> GetProductsAsync(int pageNumber, int pageSize)
{
    var query = _repository.GetAll();
    return await PaginatedList<ProductDto>.CreateAsync(
        query.ProjectTo<ProductDto>(_mapper.ConfigurationProvider),
        pageNumber,
        pageSize);
}
```

### Using Localization

```csharp
public class ProductService
{
    private readonly ILocalizationService _localizationService;

    public ProductService(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    public Result<Product> ValidateProduct(Product product)
    {
        if (string.IsNullOrEmpty(product.Name))
            return Result<Product>.Failure(_localizationService.GetString("ProductNameRequired"));

        return Result<Product>.Success(product);
    }
}
```