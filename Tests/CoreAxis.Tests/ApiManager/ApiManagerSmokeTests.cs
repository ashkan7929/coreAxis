using CoreAxis.Modules.ApiManager.Application.Services;
using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace CoreAxis.Tests.ApiManager;

public class ApiManagerSmokeTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApiManagerDbContext _dbContext;
    private readonly Mock<ILogger<ApiProxy>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

    public ApiManagerSmokeTests()
    {
        var services = new ServiceCollection();
        
        // Add in-memory database
        services.AddDbContext<ApiManagerDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
        
        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApiManagerDbContext>();
        _mockLogger = new Mock<ILogger<ApiProxy>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        
        // Ensure database is created
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task ApiManager_EndToEndWorkflow_ShouldWork()
    {
        // Arrange - Create a mock HTTP client that returns success
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"price\": 100.50, \"currency\": \"USD\"}")
            }));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.test.com")
        };

        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        // Create test data
        var webService = new WebService("TestPriceAPI", "https://api.test.com", "Test price service");
        webService.CreatedBy = "test-user";
        webService.LastModifiedBy = "test-user";
        _dbContext.WebServices.Add(webService);
        await _dbContext.SaveChangesAsync();

        var method = new WebServiceMethod(webService.Id, "/price/{symbol}", "GET", 30000);
        method.CreatedBy = "test-user";
        method.LastModifiedBy = "test-user";
        _dbContext.WebServiceMethods.Add(method);
        
        // Add a route parameter
        var parameter = new WebServiceParam(method.Id, "symbol", ParameterLocation.Route, "string", true, null);
        parameter.CreatedBy = "test-user";
        parameter.LastModifiedBy = "test-user";
        _dbContext.WebServiceParams.Add(parameter);
        
        await _dbContext.SaveChangesAsync();

        var apiProxy = new ApiProxy(_mockHttpClientFactory.Object, _mockLogger.Object, _dbContext);

        // Act - Call the API
        var parameters = new Dictionary<string, object>
        {
            { "symbol", "AAPL" }
        };
        
        var result = await apiProxy.InvokeAsync(method.Id, parameters);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Contains("price", result.ResponseBody);
        Assert.True(result.LatencyMs >= 0);
        
        // Verify that a call log was created
        var callLog = await _dbContext.WebServiceCallLogs.FirstOrDefaultAsync();
        Assert.NotNull(callLog);
        Assert.Equal(method.WebServiceId, callLog.WebServiceId);
        Assert.Equal(method.Id, callLog.MethodId);
        Assert.True(callLog.Succeeded);
        Assert.Equal(200, callLog.StatusCode);
    }

    [Fact]
    public async Task ApiManager_WithInactiveService_ShouldReturnFailure()
    {
        // Arrange
        var webService = new WebService("InactiveAPI", "https://api.test.com", "Inactive service");
        webService.CreatedBy = "test-user";
        webService.LastModifiedBy = "test-user";
        webService.Deactivate(); // Make it inactive
        _dbContext.WebServices.Add(webService);
        await _dbContext.SaveChangesAsync();

        var method = new WebServiceMethod(webService.Id, "/test", "GET", 30000);
        method.CreatedBy = "test-user";
        method.LastModifiedBy = "test-user";
        _dbContext.WebServiceMethods.Add(method);
        await _dbContext.SaveChangesAsync();

        var apiProxy = new ApiProxy(_mockHttpClientFactory.Object, _mockLogger.Object, _dbContext);

        // Act
        var result = await apiProxy.InvokeAsync(method.Id, new Dictionary<string, object>());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("WebService is inactive", result.ErrorMessage);
    }

    [Fact]
    public async Task ApiManager_WithNonExistentMethod_ShouldReturnFailure()
    {
        // Arrange
        var apiProxy = new ApiProxy(_mockHttpClientFactory.Object, _mockLogger.Object, _dbContext);
        var nonExistentMethodId = Guid.NewGuid();

        // Act
        var result = await apiProxy.InvokeAsync(nonExistentMethodId, new Dictionary<string, object>());

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("WebService method not found or inactive", result.ErrorMessage);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        _serviceProvider?.Dispose();
    }
}