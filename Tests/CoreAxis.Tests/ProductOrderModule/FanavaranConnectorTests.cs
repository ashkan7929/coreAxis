using CoreAxis.Modules.ProductOrderModule.Infrastructure.Connectors;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace CoreAxis.Tests.ProductOrderModule;

public class FanavaranConnectorTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<FanavaranConnector>> _loggerMock;
    private readonly IOptions<FanavaranOptions> _options;

    public FanavaranConnectorTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object);
        _loggerMock = new Mock<ILogger<FanavaranConnector>>();
        _options = Options.Create(new FanavaranOptions
        {
            BaseUrl = "https://example.com",
            AuthorizationHeader = "Basic xyz",
            AppName = "TestApp",
            Secret = "TestSecret",
            Username = "user",
            Password = "pass"
        });
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldUseCorrectHeaders_AndReturnCustomerId()
    {
        // Arrange
        var customerData = JsonSerializer.Serialize(new
        {
            policyholder = new
            {
                nationalId = "0011223344",
                birthDate = "13700101",
                phone = "09123456789",
                mobile = "09123456789",
                postalCode = "1234567890",
                address = new { city = "1", line = "Tehran" }
            }
        });

        // 1. Mock GetAppToken Response
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().EndsWith("/EITAuthentication/GetAppToken")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Headers = { { "appToken", "mock_app_token" } },
                Content = new StringContent("")
            });

        // 2. Mock Login Response (Verify it uses authenticationToken header in response)
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().EndsWith("/EITAuthentication/Login")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage req, CancellationToken token) =>
            {
                // Verify request headers
                if (!req.Headers.Contains("appToken")) return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                
                var resp = new HttpResponseMessage(HttpStatusCode.OK);
                resp.Headers.Add("authenticationToken", "mock_auth_token"); // The fix!
                resp.Content = new StringContent("");
                return resp;
            });

        // 3. Mock CreateCustomer Response
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().EndsWith("/BimeApi/v2.0/common/customers")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage req, CancellationToken token) =>
            {
                // Verify request headers
                if (!req.Headers.Contains("authenticationToken") || 
                    req.Headers.GetValues("authenticationToken").First() != "mock_auth_token")
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"CustomerId\": \"12345\"}")
                };
            });

        var connector = new FanavaranConnector(_loggerMock.Object, _httpClient, _options);

        // Act
        var result = await connector.CreateCustomerAsync(customerData, CancellationToken.None);

        // Assert
        Assert.Equal("12345", result);
        
        // Verify Calls
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(3), // GetAppToken, Login, CreateCustomer
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }
}
