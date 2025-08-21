# Commerce Module Tests

This directory contains comprehensive tests for the CoreAxis Commerce Module, including unit tests, integration tests, performance tests, and regression tests.

## Test Structure

```
CoreAxis.Modules.CommerceModule.Tests/
├── Unit/                           # Unit tests for individual components
│   └── CommerceUnitTests.cs       # Core unit tests
├── Integration/                    # Integration tests for component interactions
│   └── CommerceIntegrationTests.cs # End-to-end integration tests
├── Performance/                    # Performance and benchmark tests
│   ├── CommerceBenchmarkTests.cs  # BenchmarkDotNet performance tests
│   └── CommerceComparisonTests.cs # Performance comparison tests
├── Regression/                     # Regression tests for bug prevention
│   └── CommerceRegressionTests.cs # Tests for known issues and edge cases
├── Shared/                         # Shared test utilities and base classes
│   ├── CommerceTestBase.cs        # Base class for all tests
│   ├── CommerceTestUtilities.cs   # Test data generators and helpers
│   └── TestConfiguration.cs       # Test configuration management
├── TestData/                       # Test data files
├── TestOutput/                     # Test execution outputs
├── Logs/                          # Test execution logs
├── appsettings.test.json          # Test configuration
└── README.md                      # This file
```

## Test Categories

### Unit Tests
- **Inventory Management**: Stock tracking, reservations, availability calculations
- **Order Processing**: Order creation, item management, status transitions
- **Pricing & Discounts**: Price calculations, discount rules, coupon validation
- **Subscription Management**: Plan management, billing cycles, renewals
- **Payment Processing**: Payment validation, status management
- **Refund Processing**: Refund requests, validation, processing
- **Value Objects**: Money, Address, and other domain value objects
- **Domain Exceptions**: Custom exception handling and validation

### Integration Tests
- **Complete Order Flow**: End-to-end order processing with inventory and payment
- **Subscription Lifecycle**: Full subscription creation, billing, and management
- **Refund Workflows**: Complete refund processing with inventory restoration
- **Split Payment Processing**: Multi-provider payment handling
- **Payment Reconciliation**: Gateway transaction matching
- **Complex Coupon Rules**: Advanced discount scenarios
- **Multi-location Inventory**: Inventory management across locations

### Performance Tests
- **Inventory Operations**: High-volume stock operations and concurrent reservations
- **Order Processing**: Bulk order creation and processing
- **Subscription Management**: Mass subscription operations
- **Payment Processing**: High-throughput payment scenarios
- **Database Queries**: Query performance optimization
- **Memory Usage**: Large dataset handling

### Regression Tests
- **Oversell Prevention**: Inventory protection mechanisms
- **Duplicate Transaction Prevention**: Idempotency enforcement
- **Concurrency Handling**: Race condition prevention
- **Data Integrity**: Consistency validation
- **Edge Case Handling**: Boundary condition testing

## Running Tests

### Prerequisites

1. **.NET 8.0 SDK** or later
2. **SQL Server LocalDB** (for integration tests)
3. **Visual Studio 2022** or **VS Code** (optional)

### Quick Start

```bash
# Navigate to the test project directory
cd tests/Modules/CommerceModule/CoreAxis.Modules.CommerceModule.Tests

# Restore dependencies
dotnet restore

# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Running Specific Test Categories

```bash
# Unit tests only
dotnet test --filter Category=Unit

# Integration tests only
dotnet test --filter Category=Integration

# Performance tests only
dotnet test --filter Category=Performance

# Regression tests only
dotnet test --filter Category=Regression

# High priority tests only
dotnet test --filter Priority=High

# Specific feature tests
dotnet test --filter Feature=Inventory
dotnet test --filter Feature=Orders
dotnet test --filter Feature=Payments
```

### Running Performance Benchmarks

```bash
# Run BenchmarkDotNet tests
dotnet run --configuration Release --project CoreAxis.Modules.CommerceModule.Tests -- --filter *Benchmark*

# Run specific benchmark
dotnet run --configuration Release --project CoreAxis.Modules.CommerceModule.Tests -- --filter *InventoryReservationBenchmark*
```

### Running Stress Tests

```bash
# Set environment for stress testing
export COMMERCE_TEST_ENVIRONMENT=stress

# Run stress tests
dotnet test --filter Category=Stress --logger "console;verbosity=minimal"
```

## Test Configuration

### Environment Variables

- `COMMERCE_TEST_ENVIRONMENT`: Test environment (default, performance, integration, stress)
- `COMMERCE_TEST_DATABASE`: Database connection string override
- `COMMERCE_TEST_TIMEOUT`: Test timeout in milliseconds
- `COMMERCE_TEST_CONCURRENCY`: Concurrency level for stress tests
- `CI`: Set to any value when running in CI/CD

### Configuration Files

- `appsettings.test.json`: Default test configuration
- Environment-specific configurations are loaded automatically

### Database Configuration

#### In-Memory Database (Default)
```json
{
  "database": {
    "useInMemoryDatabase": true
  }
}
```

#### SQL Server LocalDB
```json
{
  "database": {
    "useInMemoryDatabase": false,
    "connectionString": "Server=(localdb)\\mssqllocaldb;Database=CoreAxisCommerceTests;Trusted_Connection=true;"
  }
}
```

## Test Data Management

### Test Data Generators

The `CommerceTestUtilities` class provides Faker-based generators for:

- **Customers**: Realistic customer data with segments
- **Inventory Items**: Products with stock levels and locations
- **Orders**: Complete orders with items and pricing
- **Subscriptions**: Plans and active subscriptions
- **Discounts**: Rules and coupons with conditions
- **Payments**: Transaction data with various statuses
- **Refunds**: Refund requests and processing data

### Example Usage

```csharp
// Generate test customer
var customer = CommerceTestUtilities.GetCustomerFaker().Generate();

// Generate inventory with specific quantity
var inventory = CommerceTestUtilities.GetInventoryItemFaker()
    .RuleFor(i => i.QuantityOnHand, 100)
    .Generate();

// Generate complete order with items
var order = CommerceTestUtilities.CreateCompleteOrder(itemCount: 5);

// Generate large dataset for performance testing
var orders = CommerceTestUtilities.CreateLargeDataset(
    CommerceTestUtilities.GetOrderFaker(), 
    count: 10000);
```

## Performance Testing

### Benchmark Tests

Benchmark tests use BenchmarkDotNet to measure:

- **Throughput**: Operations per second
- **Latency**: Response times (mean, median, percentiles)
- **Memory Usage**: Allocation patterns
- **Scalability**: Performance under load

### Performance Metrics

- **Inventory Reservations**: >1000 ops/sec
- **Order Processing**: >500 orders/sec
- **Payment Processing**: >200 payments/sec
- **Database Queries**: <100ms for complex queries
- **Memory Usage**: <1GB for 10K records

### Stress Testing

Stress tests simulate:

- **High Concurrency**: 100+ concurrent operations
- **Large Datasets**: 50K+ records
- **Extended Duration**: 10+ minute test runs
- **Resource Constraints**: Limited memory/CPU

## Continuous Integration

### GitHub Actions

```yaml
- name: Run Commerce Module Tests
  run: |
    dotnet test tests/Modules/CommerceModule/CoreAxis.Modules.CommerceModule.Tests \
      --configuration Release \
      --logger trx \
      --collect:"XPlat Code Coverage" \
      --results-directory ./TestResults

- name: Run Performance Tests
  run: |
    export COMMERCE_TEST_ENVIRONMENT=performance
    dotnet test tests/Modules/CommerceModule/CoreAxis.Modules.CommerceModule.Tests \
      --filter Category=Performance \
      --configuration Release
```

### Test Reports

- **Coverage Reports**: Generated in `TestOutput/Coverage/`
- **Performance Reports**: Generated in `TestOutput/Benchmarks/`
- **Test Results**: TRX format for CI/CD integration
- **Logs**: Structured logs in `Logs/` directory

## Troubleshooting

### Common Issues

#### Database Connection Issues
```bash
# Check LocalDB status
sqllocaldb info mssqllocaldb

# Start LocalDB if stopped
sqllocaldb start mssqllocaldb
```

#### Test Timeouts
```bash
# Increase timeout for slow tests
export COMMERCE_TEST_TIMEOUT=60000

# Run with minimal logging
dotnet test --logger "console;verbosity=minimal"
```

#### Memory Issues
```bash
# Reduce dataset size for performance tests
export COMMERCE_TEST_DATASET_SIZE=1000

# Run tests sequentially
dotnet test --parallel none
```

### Debug Mode

```bash
# Enable detailed logging
export COMMERCE_TEST_LOG_LEVEL=Debug

# Run specific test with debugging
dotnet test --filter "FullyQualifiedName~SpecificTestName" --logger "console;verbosity=detailed"
```

## Contributing

### Adding New Tests

1. **Choose appropriate category** (Unit/Integration/Performance/Regression)
2. **Inherit from CommerceTestBase** for shared functionality
3. **Use CommerceTestUtilities** for test data generation
4. **Add appropriate test traits** for categorization
5. **Include performance assertions** where applicable
6. **Document complex test scenarios**

### Test Naming Conventions

```csharp
[Fact]
[Trait(TestTraits.Category, TestCategories.Unit)]
[Trait(TestTraits.Feature, CommerceFeatures.Inventory)]
public async Task ReserveInventory_WithSufficientStock_ShouldSucceed()
{
    // Test implementation
}
```

### Performance Test Guidelines

- **Baseline measurements** before optimization
- **Realistic data volumes** for benchmarks
- **Multiple iterations** for statistical significance
- **Resource monitoring** during execution
- **Regression detection** for performance degradation

## Best Practices

### Test Design

- **Arrange-Act-Assert** pattern
- **Single responsibility** per test
- **Descriptive test names** explaining scenario
- **Independent tests** with proper cleanup
- **Realistic test data** using Faker

### Performance Considerations

- **In-memory database** for unit tests
- **Parallel execution** where safe
- **Resource cleanup** after tests
- **Minimal logging** in CI/CD
- **Efficient test data** generation

### Maintenance

- **Regular test review** for relevance
- **Performance baseline** updates
- **Test data refresh** for realism
- **Documentation updates** with changes
- **Flaky test** identification and fixing

## Support

For questions or issues with the Commerce Module tests:

1. Check this README for common solutions
2. Review test logs in the `Logs/` directory
3. Check CI/CD pipeline results
4. Contact the Commerce Module team
5. Create an issue in the project repository

---

**Last Updated**: December 2024  
**Version**: 1.0.0  
**Maintainer**: Commerce Module Team