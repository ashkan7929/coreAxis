using CoreAxis.Modules.ApiManager.Domain;
using CoreAxis.Modules.ApiManager.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CoreAxis.Tests.ApiManager
{
    public class ApiManagerIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ApiManagerDbContext _dbContext;

        public ApiManagerIntegrationTests()
        {
            var services = new ServiceCollection();
            
            // Add in-memory database
            services.AddDbContext<ApiManagerDbContext>(options =>
                options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));
            
            _serviceProvider = services.BuildServiceProvider();
            _dbContext = _serviceProvider.GetRequiredService<ApiManagerDbContext>();
            
            // Ensure database is created
            _dbContext.Database.EnsureCreated();
        }

        [Fact]
        public async Task WebService_CRUD_Operations_ShouldWorkCorrectly()
        {
            // Create
            var webService = new WebService("TestAPI", "https://api.test.com", "Test API Description");
            webService.CreatedBy = "test-user";
            webService.LastModifiedBy = "test-user";
            _dbContext.WebServices.Add(webService);
            await _dbContext.SaveChangesAsync();

            // Read
            var retrievedService = await _dbContext.WebServices
                .FirstOrDefaultAsync(ws => ws.Name == "TestAPI");
            
            Assert.NotNull(retrievedService);
            Assert.Equal("TestAPI", retrievedService.Name);
            Assert.Equal("https://api.test.com", retrievedService.BaseUrl);
            Assert.True(retrievedService.IsActive);

            // Update
            retrievedService.Update("UpdatedAPI", "https://updated.api.com", "Updated Description");
            await _dbContext.SaveChangesAsync();

            var updatedService = await _dbContext.WebServices.FindAsync(retrievedService.Id);
            Assert.Equal("UpdatedAPI", updatedService.Name);
            Assert.Equal("https://updated.api.com", updatedService.BaseUrl);

            // Deactivate
            updatedService.Deactivate();
            await _dbContext.SaveChangesAsync();

            var deactivatedService = await _dbContext.WebServices.FindAsync(retrievedService.Id);
            Assert.False(deactivatedService.IsActive);
        }

        [Fact]
        public async Task WebServiceMethod_WithWebService_ShouldMaintainRelationship()
        {
            // Arrange
            var webService = new WebService("TestAPI", "https://api.test.com");
            webService.CreatedBy = "test-user";
            webService.LastModifiedBy = "test-user";
            _dbContext.WebServices.Add(webService);
            await _dbContext.SaveChangesAsync();

            var method = new WebServiceMethod(webService.Id, "/users", "GET", 30000);
            method.CreatedBy = "test-user";
            method.LastModifiedBy = "test-user";
            _dbContext.WebServiceMethods.Add(method);
            await _dbContext.SaveChangesAsync();

            // Act
            var retrievedMethod = await _dbContext.WebServiceMethods
                .Include(m => m.WebService)
                .FirstOrDefaultAsync(m => m.Path == "/users");

            // Assert
            Assert.NotNull(retrievedMethod);
            Assert.Equal(webService.Id, retrievedMethod.WebServiceId);
            Assert.NotNull(retrievedMethod.WebService);
            Assert.Equal("TestAPI", retrievedMethod.WebService.Name);
        }

        [Fact]
        public async Task SecurityProfile_WithWebService_ShouldWorkCorrectly()
        {
            // Arrange
            var configJson = "{\"headerName\": \"X-API-Key\", \"apiKey\": \"test-key\"}";
            var securityProfile = new SecurityProfile(SecurityType.ApiKey, configJson);
            securityProfile.CreatedBy = "test-user";
            securityProfile.LastModifiedBy = "test-user";
            _dbContext.SecurityProfiles.Add(securityProfile);
            await _dbContext.SaveChangesAsync();

            var webService = new WebService("SecureAPI", "https://secure.api.com", 
                "Secure API", securityProfile.Id);
            webService.CreatedBy = "test-user";
            webService.LastModifiedBy = "test-user";
            _dbContext.WebServices.Add(webService);
            await _dbContext.SaveChangesAsync();

            // Act
            var retrievedService = await _dbContext.WebServices
                .Include(ws => ws.SecurityProfile)
                .FirstOrDefaultAsync(ws => ws.Name == "SecureAPI");

            // Assert
            Assert.NotNull(retrievedService);
            Assert.NotNull(retrievedService.SecurityProfile);
            Assert.Equal(SecurityType.ApiKey, retrievedService.SecurityProfile.Type);
            Assert.Equal(configJson, retrievedService.SecurityProfile.ConfigJson);
        }

        [Fact]
        public async Task WebServiceCallLog_Creation_ShouldStoreCorrectly()
        {
            // Arrange
            var webService = new WebService("LogTestAPI", "https://log.test.com");
            webService.CreatedBy = "test-user";
            webService.LastModifiedBy = "test-user";
            _dbContext.WebServices.Add(webService);
            await _dbContext.SaveChangesAsync();

            var method = new WebServiceMethod(webService.Id, "/test", "POST", 15000);
            method.CreatedBy = "test-user";
            method.LastModifiedBy = "test-user";
            _dbContext.WebServiceMethods.Add(method);
            await _dbContext.SaveChangesAsync();

            var correlationId = Guid.NewGuid().ToString();
            var callLog = new WebServiceCallLog(webService.Id, method.Id, correlationId);
            callLog.CreatedBy = "test-user";
            callLog.LastModifiedBy = "test-user";
            callLog.SetRequest("POST /test HTTP/1.1\nContent-Type: application/json\n\n{\"test\": true}");
            callLog.SetResponse("{\"success\": true}", 200, 150, true);
            
            _dbContext.WebServiceCallLogs.Add(callLog);
            await _dbContext.SaveChangesAsync();

            // Act
            var retrievedLog = await _dbContext.WebServiceCallLogs
                .FirstOrDefaultAsync(log => log.CorrelationId == correlationId);

            // Assert
            Assert.NotNull(retrievedLog);
            Assert.Equal(webService.Id, retrievedLog.WebServiceId);
            Assert.Equal(method.Id, retrievedLog.MethodId);
            Assert.Equal(200, retrievedLog.StatusCode);
            Assert.True(retrievedLog.Succeeded);
            Assert.Equal(150, retrievedLog.LatencyMs);
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
            _serviceProvider?.Dispose();
        }
    }
}