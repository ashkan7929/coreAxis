# DemoModule

## Purpose

The DemoModule serves as a reference implementation and demonstration of the CoreAxis modular architecture. It showcases how to properly implement a module following Clean Architecture principles, SOLID design patterns, and the CoreAxis module development guidelines. This module provides a complete example of managing demo items with full CRUD operations, pagination, categorization, and event-driven communication.

## Architecture

This module follows the Clean Architecture pattern with the following layers:

- **Domain** (`CoreAxis.Modules.DemoModule.Domain`): Contains business entities, value objects, domain events, and repository interfaces.
- **Application** (`CoreAxis.Modules.DemoModule.Application`): Contains application services, use cases, and domain event handlers.
- **Infrastructure** (`CoreAxis.Modules.DemoModule.Infrastructure`): Contains implementations of repository interfaces, external services, and data access.
- **API** (`CoreAxis.Modules.DemoModule.API`): Contains controllers, DTOs, and API-specific logic.

```
DemoModule/
├── Domain/
│   ├── Entities/
│   │   └── DemoItem.cs
│   ├── Events/
│   │   ├── DemoItemCreatedEvent.cs
│   │   ├── DemoItemUpdatedEvent.cs
│   │   └── DemoItemFeaturedChangedEvent.cs
│   └── Interfaces/
│       └── IDemoItemRepository.cs
├── Application/
│   └── Services/
│       ├── IDemoItemService.cs
│       └── DemoItemService.cs
├── Infrastructure/
│   ├── Repositories/
│   │   └── DemoItemRepository.cs
│   └── Configurations/
│       └── DemoItemConfiguration.cs
└── API/
    ├── Controllers/
    │   └── DemoItemsController.cs
    └── DemoModuleRegistration.cs
```

## Entities

### DemoItem

The main domain entity representing a demo item in the system.

- **Properties**:
  - Id (Guid): Unique identifier (inherited from EntityBase)
  - Name (string): Name of the demo item (required, max 200 characters)
  - Description (string): Description of the demo item (required, max 1000 characters)
  - Price (decimal): Price of the demo item (must be positive)
  - Category (string): Category of the demo item (required, max 100 characters)
  - IsFeatured (bool): Whether the demo item is featured (default: false)
  - CreatedOn (DateTime): Creation timestamp (inherited from EntityBase)
  - LastModifiedOn (DateTime): Last modification timestamp (inherited from EntityBase)
  - IsActive (bool): Whether the entity is active (inherited from EntityBase)

- **Behaviors**:
  - `Create(name, description, price, category)`: Creates a new demo item with validation
  - `Update(name, description, price, category)`: Updates an existing demo item
  - `SetFeatured(isFeatured)`: Sets whether the demo item is featured
  - `Deactivate()`: Marks the demo item as inactive

- **Domain Events**:
  - `DemoItemCreatedEvent`: Raised when a demo item is created
  - `DemoItemUpdatedEvent`: Raised when a demo item is updated
  - `DemoItemFeaturedChangedEvent`: Raised when a demo item's featured status is changed

- **Business Rules**:
  - Name cannot be null or empty
  - Description cannot be null or empty
  - Price must be greater than zero
  - Category cannot be null or empty

## Events

### Domain Events

#### DemoItemCreatedEvent

- **Properties**:
  - DemoItemId (Guid): ID of the created demo item
  - Name (string): Name of the created demo item
  - Category (string): Category of the created demo item
  - Price (decimal): Price of the created demo item

- **Purpose**: Notifies domain event handlers that a new demo item has been created. Used for internal domain logic and can trigger integration events.

#### DemoItemUpdatedEvent

- **Properties**:
  - DemoItemId (Guid): ID of the updated demo item
  - Name (string): Updated name of the demo item
  - Category (string): Updated category of the demo item
  - Price (decimal): Updated price of the demo item

- **Purpose**: Notifies domain event handlers that a demo item has been updated.

#### DemoItemFeaturedChangedEvent

- **Properties**:
  - DemoItemId (Guid): ID of the demo item
  - IsFeatured (bool): New featured status

- **Purpose**: Notifies domain event handlers when a demo item's featured status changes.

### Integration Events

#### DemoItemCreatedIntegrationEvent

- **Properties**:
  - DemoItemId (Guid): ID of the created demo item
  - Name (string): Name of the created demo item
  - Category (string): Category of the created demo item
  - Price (decimal): Price of the created demo item
  - CreatedOn (DateTime): Creation timestamp

- **Purpose**: Notifies other modules that a new demo item has been created. Other modules can subscribe to this event to perform their own business logic.

## APIs

### DemoItems Controller

Base route: `/api/demoitems`

#### GET /api/demoitems

- **Description**: Gets all demo items with pagination.
- **Query Parameters**:
  - pageNumber (int, optional): The page number. Default: 1.
  - pageSize (int, optional): The page size. Default: 10.
- **Response**: A paginated list of demo items.

```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Demo Item 1",
      "description": "This is a demo item",
      "price": 10.99,
      "category": "Electronics",
      "isFeatured": false,
      "createdOn": "2024-01-01T00:00:00Z",
      "lastModifiedOn": "2024-01-01T00:00:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

#### GET /api/demoitems/{id}

- **Description**: Gets a specific demo item by ID.
- **Path Parameters**:
  - id (Guid): The ID of the demo item.
- **Response**: The demo item details or 404 if not found.

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Demo Item 1",
  "description": "This is a demo item",
  "price": 10.99,
  "category": "Electronics",
  "isFeatured": false,
  "createdOn": "2024-01-01T00:00:00Z",
  "lastModifiedOn": "2024-01-01T00:00:00Z"
}
```

#### GET /api/demoitems/category/{category}

- **Description**: Gets demo items by category with pagination.
- **Path Parameters**:
  - category (string): The category to filter by.
- **Query Parameters**:
  - pageNumber (int, optional): The page number. Default: 1.
  - pageSize (int, optional): The page size. Default: 10.
- **Response**: A paginated list of demo items in the specified category.

#### GET /api/demoitems/featured

- **Description**: Gets all featured demo items.
- **Response**: A collection of featured demo items.

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Featured Demo Item",
    "description": "This is a featured demo item",
    "price": 29.99,
    "category": "Electronics",
    "isFeatured": true,
    "createdOn": "2024-01-01T00:00:00Z",
    "lastModifiedOn": "2024-01-01T00:00:00Z"
  }
]
```

#### POST /api/demoitems

- **Description**: Creates a new demo item.
- **Request Body**:

```json
{
  "name": "New Demo Item",
  "description": "This is a new demo item",
  "price": 15.99,
  "category": "Books"
}
```

- **Response**: The created demo item with 201 status code.

#### PUT /api/demoitems/{id}

- **Description**: Updates an existing demo item.
- **Path Parameters**:
  - id (Guid): The ID of the demo item to update.
- **Request Body**:

```json
{
  "name": "Updated Demo Item",
  "description": "This is an updated demo item",
  "price": 19.99,
  "category": "Books"
}
```

- **Response**: The updated demo item or 404 if not found.

#### PUT /api/demoitems/{id}/featured

- **Description**: Sets the featured status of a demo item.
- **Path Parameters**:
  - id (Guid): The ID of the demo item.
- **Request Body**:

```json
{
  "isFeatured": true
}
```

- **Response**: 204 No Content on success or 404 if not found.

#### DELETE /api/demoitems/{id}

- **Description**: Deletes (deactivates) a demo item.
- **Path Parameters**:
  - id (Guid): The ID of the demo item to delete.
- **Response**: 204 No Content on success or 404 if not found.

## Setup Instructions

### Prerequisites

- .NET 9.0 SDK
- Entity Framework Core
- CoreAxis.SharedKernel package
- CoreAxis.EventBus package

### Installation

1. The DemoModule is automatically discovered and registered by the CoreAxis module system.
2. No additional configuration is required as it uses in-memory storage for demonstration purposes.
3. The module will be available at startup and its endpoints will be mapped automatically.

### Configuration

The DemoModule uses the following configuration sections:

```json
{
  "DemoModule": {
    "EnableFeatures": true,
    "DefaultPageSize": 10,
    "MaxPageSize": 100
  }
}
```

### Database Setup

For production use, configure Entity Framework with your preferred database provider:

1. Add connection string to appsettings.json
2. Configure DbContext in DemoModuleRegistration
3. Run migrations to create the database schema

### Testing

The module includes comprehensive tests:

- **Unit Tests**: `CoreAxis.Tests/DemoModule/`
  - DemoItemTests.cs
  - DemoItemServiceTests.cs
  - DemoItemsControllerTests.cs

- **Integration Tests**: API endpoint testing with in-memory database

Run tests using:
```bash
dotnet test
```

### Example Usage

```bash
# Get all demo items
curl -X GET "https://localhost:5001/api/demoitems?pageNumber=1&pageSize=10"

# Create a new demo item
curl -X POST "https://localhost:5001/api/demoitems" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Sample Item",
    "description": "A sample demo item",
    "price": 25.99,
    "category": "Sample"
  }'

# Get featured items
curl -X GET "https://localhost:5001/api/demoitems/featured"
```

## Dependencies

- CoreAxis.SharedKernel: Base classes and common abstractions
- CoreAxis.EventBus: Event publishing and handling
- Microsoft.EntityFrameworkCore: Data access
- Microsoft.AspNetCore.Mvc: Web API framework
- MediatR: In-process messaging

## Contributing

When extending or modifying the DemoModule:

1. Follow the established Clean Architecture patterns
2. Maintain SOLID principles
3. Add appropriate unit and integration tests
4. Update this documentation
5. Ensure all domain events are properly published
6. Follow the CoreAxis coding standards and conventions

## Notes

- This module serves as a reference implementation for other modules
- It demonstrates proper use of domain events and integration events
- The implementation uses in-memory storage for simplicity
- For production modules, implement proper data persistence
- All operations return Result<T> for consistent error handling
- The module is fully isolated and communicates only through events and SharedKernel