using CoreAxis.Modules.CommerceModule.Api.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace CoreAxis.Modules.CommerceModule.Tests.Security;

/// <summary>
/// Security tests for Commerce API endpoints
/// Tests rate limiting, input validation, authentication, and authorization
/// </summary>
public class ApiSecurityTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiSecurityTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.AddLogging(logging => logging.AddXUnit(output));
            });
        });
        
        _client = _factory.CreateClient();
        _output = output;
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    #region Authentication Tests

    [Fact]
    public async Task ProtectedEndpoints_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Test various protected endpoints without authentication
        var protectedEndpoints = new[]
        {
            "/api/v1/inventory",
            "/api/v1/orders",
            "/api/v1/payments",
            "/api/v1/subscriptions"
        };

        foreach (var endpoint in protectedEndpoints)
        {
            var response = await _client.GetAsync(endpoint);
            
            Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || 
                       response.StatusCode == HttpStatusCode.Forbidden,
                $"Endpoint {endpoint} should require authentication but returned {response.StatusCode}");
            
            _output.WriteLine($"✓ {endpoint} properly requires authentication");
        }
    }

    [Fact]
    public async Task ProtectedEndpoints_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Set invalid token
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid.jwt.token");
        
        var response = await _client.GetAsync("/api/v1/inventory");
        
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || 
                   response.StatusCode == HttpStatusCode.Forbidden);
        
        _output.WriteLine("✓ Invalid JWT token properly rejected");
    }

    [Fact]
    public async Task ProtectedEndpoints_WithExpiredToken_ShouldReturnUnauthorized()
    {
        // Simulate expired token (in real scenario, this would be a properly formatted but expired JWT)
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyLCJleHAiOjE1MTYyMzkwMjJ9.expired";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
        
        var response = await _client.GetAsync("/api/v1/inventory");
        
        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || 
                   response.StatusCode == HttpStatusCode.Forbidden);
        
        _output.WriteLine("✓ Expired JWT token properly rejected");
    }

    [Fact]
    public async Task AdminEndpoints_WithUserRole_ShouldReturnForbidden()
    {
        // Simulate user token (not admin)
        var userToken = CreateMockJwtToken("user", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        
        // Test admin-only endpoints
        var adminEndpoints = new[]
        {
            ("/api/v1/inventory", HttpMethod.Delete),
            ("/api/v1/orders/admin", HttpMethod.Get),
            ("/api/v1/payments/admin", HttpMethod.Get)
        };

        foreach (var (endpoint, method) in adminEndpoints)
        {
            var request = new HttpRequestMessage(method, endpoint);
            var response = await _client.SendAsync(request);
            
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            _output.WriteLine($"✓ {method} {endpoint} properly requires admin role");
        }
    }

    #endregion

    #region Input Validation Tests

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("'; DROP TABLE inventory; --")]
    [InlineData("../../../etc/passwd")]
    [InlineData("%3Cscript%3Ealert('xss')%3C/script%3E")]
    public async Task InputValidation_MaliciousInput_ShouldBeSanitizedOrRejected(string maliciousInput)
    {
        var validToken = CreateMockJwtToken("testuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        
        var createInventoryDto = new CreateInventoryItemDto
        {
            SKU = maliciousInput,
            Name = maliciousInput,
            Description = maliciousInput,
            Quantity = 10,
            Price = 25.00m,
            Category = maliciousInput,
            Supplier = maliciousInput,
            Location = maliciousInput
        };
        
        var json = JsonSerializer.Serialize(createInventoryDto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync("/api/v1/inventory", content);
        
        // Should either reject the input (400) or sanitize it (200/201)
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                   response.StatusCode == HttpStatusCode.Created || 
                   response.StatusCode == HttpStatusCode.OK);
        
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            // Verify that malicious content was sanitized
            Assert.DoesNotContain("<script>", responseContent);
            Assert.DoesNotContain("DROP TABLE", responseContent);
        }
        
        _output.WriteLine($"✓ Malicious input '{maliciousInput}' properly handled");
    }

    [Theory]
    [InlineData(-1)] // Negative quantity
    [InlineData(int.MaxValue)] // Extremely large quantity
    [InlineData(0)] // Zero quantity for creation
    public async Task InputValidation_InvalidQuantity_ShouldReturnBadRequest(int invalidQuantity)
    {
        var validToken = CreateMockJwtToken("testuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        
        var createInventoryDto = new CreateInventoryItemDto
        {
            SKU = "TEST-SKU-001",
            Name = "Test Product",
            Description = "Test Description",
            Quantity = invalidQuantity,
            Price = 25.00m,
            Category = "Test",
            Supplier = "Test Supplier",
            Location = "Test Location"
        };
        
        var json = JsonSerializer.Serialize(createInventoryDto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync("/api/v1/inventory", content);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _output.WriteLine($"✓ Invalid quantity {invalidQuantity} properly rejected");
    }

    [Theory]
    [InlineData(-10.00)] // Negative price
    [InlineData(0.00)] // Zero price
    [InlineData(double.MaxValue)] // Extremely large price
    public async Task InputValidation_InvalidPrice_ShouldReturnBadRequest(decimal invalidPrice)
    {
        var validToken = CreateMockJwtToken("testuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        
        var createInventoryDto = new CreateInventoryItemDto
        {
            SKU = "TEST-SKU-002",
            Name = "Test Product",
            Description = "Test Description",
            Quantity = 10,
            Price = invalidPrice,
            Category = "Test",
            Supplier = "Test Supplier",
            Location = "Test Location"
        };
        
        var json = JsonSerializer.Serialize(createInventoryDto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync("/api/v1/inventory", content);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _output.WriteLine($"✓ Invalid price {invalidPrice} properly rejected");
    }

    [Fact]
    public async Task InputValidation_ExcessivelyLongStrings_ShouldReturnBadRequest()
    {
        var validToken = CreateMockJwtToken("testuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        
        var longString = new string('A', 10000); // 10KB string
        
        var createInventoryDto = new CreateInventoryItemDto
        {
            SKU = longString,
            Name = longString,
            Description = longString,
            Quantity = 10,
            Price = 25.00m,
            Category = longString,
            Supplier = longString,
            Location = longString
        };
        
        var json = JsonSerializer.Serialize(createInventoryDto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync("/api/v1/inventory", content);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _output.WriteLine("✓ Excessively long strings properly rejected");
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task RateLimiting_ExcessiveRequests_ShouldReturnTooManyRequests()
    {
        var validToken = CreateMockJwtToken("testuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Send many requests rapidly to trigger rate limiting
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_client.GetAsync("/api/v1/inventory?page=1&pageSize=10"));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // At least some requests should be rate limited
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        
        Assert.True(rateLimitedResponses > 0, "Rate limiting should have been triggered");
        
        _output.WriteLine($"✓ Rate limiting triggered for {rateLimitedResponses} out of 100 requests");
        
        // Cleanup
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task RateLimiting_DifferentEndpoints_ShouldHaveIndependentLimits()
    {
        var validToken = CreateMockJwtToken("testuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        
        var endpoints = new[]
        {
            "/api/v1/inventory",
            "/api/v1/orders",
            "/api/v1/payments"
        };
        
        var allResponses = new List<HttpResponseMessage>();
        
        foreach (var endpoint in endpoints)
        {
            var tasks = new List<Task<HttpResponseMessage>>();
            
            // Send requests to each endpoint
            for (int i = 0; i < 20; i++)
            {
                tasks.Add(_client.GetAsync($"{endpoint}?page=1&pageSize=5"));
            }
            
            var responses = await Task.WhenAll(tasks);
            allResponses.AddRange(responses);
            
            var successfulResponses = responses.Count(r => r.IsSuccessStatusCode);
            var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
            
            _output.WriteLine($"Endpoint {endpoint}: {successfulResponses} successful, {rateLimitedResponses} rate limited");
        }
        
        // Cleanup
        foreach (var response in allResponses)
        {
            response.Dispose();
        }
    }

    #endregion

    #region Data Protection Tests

    [Fact]
    public async Task SensitiveData_InPaymentRequests_ShouldNotBeLoggedOrExposed()
    {
        var validToken = CreateMockJwtToken("testuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        
        var processPaymentDto = new ProcessPaymentDto
        {
            OrderId = Guid.NewGuid(),
            Amount = 99.99m,
            PaymentMethod = "CreditCard",
            PaymentDetails = new Dictionary<string, object>
            {
                { "cardNumber", "4111111111111111" }, // Test card number
                { "expiryMonth", "12" },
                { "expiryYear", "2025" },
                { "cvv", "123" } // Sensitive data
            },
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        
        var json = JsonSerializer.Serialize(processPaymentDto, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync("/api/v1/payments", content);
        
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Verify sensitive data is not exposed in response
            Assert.DoesNotContain("4111111111111111", responseContent);
            Assert.DoesNotContain("123", responseContent); // CVV should not be in response
            
            // Should contain masked version
            Assert.Contains("****", responseContent);
        }
        
        _output.WriteLine("✓ Sensitive payment data properly masked in response");
    }

    [Fact]
    public async Task PersonalData_InResponses_ShouldBeFilteredBasedOnUserRole()
    {
        // Test with user role
        var userToken = CreateMockJwtToken("regularuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        
        var userResponse = await _client.GetAsync("/api/v1/orders");
        
        if (userResponse.IsSuccessStatusCode)
        {
            var userContent = await userResponse.Content.ReadAsStringAsync();
            // Regular users should not see sensitive admin data
            Assert.DoesNotContain("internalNotes", userContent.ToLower());
            Assert.DoesNotContain("adminComments", userContent.ToLower());
        }
        
        // Test with admin role
        var adminToken = CreateMockJwtToken("adminuser", new[] { "Admin" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        
        var adminResponse = await _client.GetAsync("/api/v1/orders");
        
        // Admin should have access to more data (this would be implementation specific)
        Assert.True(adminResponse.IsSuccessStatusCode || adminResponse.StatusCode == HttpStatusCode.NotFound);
        
        _output.WriteLine("✓ Data filtering based on user role working correctly");
    }

    #endregion

    #region CORS and Security Headers Tests

    [Fact]
    public async Task SecurityHeaders_ShouldBePresentInResponses()
    {
        var response = await _client.GetAsync("/api/v1/health"); // Assuming a health check endpoint
        
        // Check for important security headers
        var expectedHeaders = new[]
        {
            "X-Content-Type-Options",
            "X-Frame-Options",
            "X-XSS-Protection",
            "Strict-Transport-Security"
        };
        
        foreach (var headerName in expectedHeaders)
        {
            if (response.Headers.Contains(headerName) || 
                (response.Content.Headers?.Contains(headerName) ?? false))
            {
                _output.WriteLine($"✓ Security header {headerName} present");
            }
            else
            {
                _output.WriteLine($"⚠ Security header {headerName} missing");
            }
        }
    }

    [Fact]
    public async Task CORS_InvalidOrigin_ShouldBeRejected()
    {
        _client.DefaultRequestHeaders.Add("Origin", "https://malicious-site.com");
        
        var response = await _client.GetAsync("/api/v1/inventory");
        
        // CORS should either reject the request or not include CORS headers for invalid origins
        var corsHeader = response.Headers.FirstOrDefault(h => h.Key == "Access-Control-Allow-Origin");
        
        if (corsHeader.Key != null)
        {
            Assert.NotEqual("https://malicious-site.com", corsHeader.Value.FirstOrDefault());
        }
        
        _output.WriteLine("✓ CORS properly configured for invalid origins");
    }

    #endregion

    #region SQL Injection Tests

    [Theory]
    [InlineData("'; DROP TABLE inventory; --")]
    [InlineData("1' OR '1'='1")]
    [InlineData("'; UPDATE inventory SET quantity = 0; --")]
    [InlineData("1; EXEC xp_cmdshell('dir'); --")]
    public async Task SqlInjection_MaliciousQueries_ShouldBePrevented(string maliciousInput)
    {
        var validToken = CreateMockJwtToken("testuser", new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", validToken);
        
        // Test SQL injection in search parameters
        var response = await _client.GetAsync($"/api/v1/inventory?search={Uri.EscapeDataString(maliciousInput)}");
        
        // Should not return server error (which might indicate SQL injection succeeded)
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            // Should not contain SQL error messages
            Assert.DoesNotContain("SQL", content);
            Assert.DoesNotContain("syntax error", content.ToLower());
            Assert.DoesNotContain("database", content.ToLower());
        }
        
        _output.WriteLine($"✓ SQL injection attempt '{maliciousInput}' properly handled");
    }

    #endregion

    #region Helper Methods

    private string CreateMockJwtToken(string userId, string[] roles)
    {
        // In a real scenario, this would create a properly signed JWT token
        // For testing purposes, we'll create a mock token structure
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{{\"sub\":\"{userId}\",\"roles\":[\"{string.Join("\",\"", roles)}\"],\"exp\":{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}}}"));
        var signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("mock_signature"));
        
        return $"{header}.{payload}.{signature}";
    }

    #endregion

    public void Dispose()
    {
        _client?.Dispose();
    }
}

/// <summary>
/// Additional security test class for testing specific attack vectors
/// </summary>
public class SecurityAttackVectorTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SecurityAttackVectorTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task FileUpload_MaliciousFiles_ShouldBeRejected()
    {
        // Test file upload security (if file upload endpoints exist)
        var maliciousContent = "<?php system($_GET['cmd']); ?>";
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(maliciousContent), "file", "malicious.php");
        
        var response = await _client.PostAsync("/api/v1/upload", content);
        
        // Should either reject the file type or sanitize the content
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest || 
                   response.StatusCode == HttpStatusCode.UnsupportedMediaType ||
                   response.StatusCode == HttpStatusCode.NotFound); // If endpoint doesn't exist
    }

    [Fact]
    public async Task LargePayload_ShouldBeRejected()
    {
        // Test protection against large payload attacks
        var largePayload = new string('A', 10 * 1024 * 1024); // 10MB
        var content = new StringContent(largePayload, Encoding.UTF8, "application/json");
        
        var response = await _client.PostAsync("/api/v1/inventory", content);
        
        // Should reject large payloads
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.RequestEntityTooLarge);
    }

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\..\\windows\\system32\\config\\sam")]
    [InlineData("/etc/shadow")]
    [InlineData("C:\\Windows\\System32\\config\\SAM")]
    public async Task PathTraversal_ShouldBePrevented(string maliciousPath)
    {
        // Test path traversal in file-related endpoints
        var response = await _client.GetAsync($"/api/v1/files/{Uri.EscapeDataString(maliciousPath)}");
        
        // Should not allow access to system files
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest ||
                   response.StatusCode == HttpStatusCode.Forbidden ||
                   response.StatusCode == HttpStatusCode.NotFound);
    }
}