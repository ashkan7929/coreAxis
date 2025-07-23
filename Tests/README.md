# Tests

## Purpose

The Tests project contains unit tests and integration tests for all system components. It ensures that the system works correctly and complies with requirements.

## Types of Tests

### Unit Tests

Test individual units of code in isolation from the rest of the system. Use Moq to mock dependencies.

- **SharedKernel**: Tests for shared kernel components
- **EventBus**: Tests for the event bus system
- **ApiGateway**: Tests for the API Gateway
- **DemoModule**: Tests for the demo module

### Integration Tests

Test the interaction of different components together. Use an in-memory database.

## How to Run Tests

### Run All Tests

```bash
dotnet test
```

### Run a Specific Group of Tests

```bash
dotnet test --filter "Category=UnitTest"
```

### Run Specific Unit Tests

```bash
dotnet test --filter "FullyQualifiedName~DemoItemTests"
```

## Adding New Tests

### New Unit Test

```csharp
public class NewServiceTests
{
    private readonly Mock<IDependency> _dependencyMock;
    private readonly NewService _sut; // System Under Test

    public NewServiceTests()
    {
        _dependencyMock = new Mock<IDependency>();
        _sut = new NewService(_dependencyMock.Object);
    }

    [Fact]
    public async Task MethodName_Condition_ExpectedResult()
    {
        // Arrange
        _dependencyMock.Setup(d => d.MethodName()).ReturnsAsync(expectedValue);

        // Act
        var result = await _sut.MethodToTest();

        // Assert
        Assert.Equal(expectedValue, result);
        _dependencyMock.Verify(d => d.MethodName(), Times.Once);
    }
}
```

### New Integration Test

```csharp
public class NewIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public NewIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Configure test services
            });
        });
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Endpoint_Condition_ExpectedResult()
    {
        // Arrange
        
        // Act
        var response = await _client.GetAsync("/api/endpoint");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        // Verify content
    }
}
```