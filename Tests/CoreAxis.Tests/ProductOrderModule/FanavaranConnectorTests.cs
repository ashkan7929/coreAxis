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

    [Fact]
    public async Task GetUniversalLifePriceAsync_ShouldHandleBeneficiariesAndSurchargesCorrectly()
    {
        // Arrange
        var customerId = "1001";
        var appData = new
        {
            contract = new { annualPremium = 1000000, durationYears = 20 },
            coverage = new { },
            body = new { heightCm = 180, weightKg = 80 },
            policyholder = new { mainJob = 1 },
            InsuredPeople = new[]
            {
                new
                {
                    InsuredPersonId = 1001,
                    Beneficiaries = new[]
                    {
                        new { BeneficiaryRelationId = 103, BeneficiaryId = (long?)999 }, // Should become null
                        new { BeneficiaryRelationId = 117, BeneficiaryId = (long?)888 }, // Should become null
                        new { BeneficiaryRelationId = 101, BeneficiaryId = (long?)null }, // Should become 1001
                        new { BeneficiaryRelationId = 105, BeneficiaryId = (long?)777 } // Should remain 777
                    }
                }
            }
        };
        var applicationData = JsonSerializer.Serialize(appData);

        // Mock Token & Login
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().Contains("GetAppToken")), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Headers = { { "appToken", "mock_app" } } });
        
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().Contains("Login")), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Headers = { { "authenticationToken", "mock_auth" } } });

        // Mock Price Calculation
        string capturedJson = null;
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().Contains("Universal-life-policies")), ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage req, CancellationToken token) =>
            {
                if (req.Content is MultipartFormDataContent multipart)
                {
                    foreach (var part in multipart)
                    {
                        var str = await part.ReadAsStringAsync();
                        if (str.Trim().StartsWith("{") && str.Contains("InsuredPeople"))
                        {
                            capturedJson = str;
                            break;
                        }
                    }
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("1000000") };
            });

        var connector = new FanavaranConnector(_loggerMock.Object, _httpClient, _options);

        // Act
        await connector.GetUniversalLifePriceAsync(customerId, applicationData, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedJson);
        var jsonDoc = JsonDocument.Parse(capturedJson);
        var insuredPerson = jsonDoc.RootElement.GetProperty("InsuredPeople")[0];
        var beneficiaries = insuredPerson.GetProperty("Beneficiaries");

        // Check Relation 103 -> BeneficiaryId should be missing (null)
        var ben103 = beneficiaries.EnumerateArray().First(b => b.GetProperty("BeneficiaryRelationId").GetInt32() == 103);
        Assert.False(ben103.TryGetProperty("BeneficiaryId", out _), "BeneficiaryId should be absent for Relation 103");

        // Check Relation 117 -> BeneficiaryId should be missing (null)
        var ben117 = beneficiaries.EnumerateArray().First(b => b.GetProperty("BeneficiaryRelationId").GetInt32() == 117);
        Assert.False(ben117.TryGetProperty("BeneficiaryId", out _), "BeneficiaryId should be absent for Relation 117");

        // Check Relation 101 -> BeneficiaryId should be 1001 (auto-populated)
        var ben101 = beneficiaries.EnumerateArray().First(b => b.GetProperty("BeneficiaryRelationId").GetInt32() == 101);
        Assert.True(ben101.TryGetProperty("BeneficiaryId", out var id101));
        Assert.Equal(1001, id101.GetInt64());

        // Check Relation 105 -> BeneficiaryId should be 777 (preserved)
        var ben105 = beneficiaries.EnumerateArray().First(b => b.GetProperty("BeneficiaryRelationId").GetInt32() == 105);
        Assert.True(ben105.TryGetProperty("BeneficiaryId", out var id105));
        Assert.Equal(777, id105.GetInt64());
        
        // Check Surcharges
        var surcharges = insuredPerson.GetProperty("Surcharges");
        var surcharge = surcharges[0];
        Assert.Equal(19, surcharge.GetProperty("SurchargeId").GetInt32());
        Assert.True(surcharge.TryGetProperty("ExerciseDuration", out var ed));
        Assert.Equal("5", ed.GetString());
    }
}
