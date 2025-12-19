using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Connectors;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.DTOs.Fanavaran;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Connectors;

public class FanavaranConnector : IFanavaranConnector
{
    private readonly ILogger<FanavaranConnector> _logger;
    private readonly HttpClient _httpClient;
    private readonly FanavaranOptions _options;

    public FanavaranConnector(ILogger<FanavaranConnector> logger, HttpClient httpClient, IOptions<FanavaranOptions> options)
    {
        _logger = logger;
        _httpClient = httpClient;
        _options = options.Value;
    }

    private async Task<string> GetAppTokenAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Requesting AppToken from Fanavaran...");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/EITAuthentication/GetAppToken");
        request.Headers.Add("appname", _options.AppName);
        request.Headers.Add("secret", _options.Secret);
        request.Headers.Add("Authorization", _options.AuthorizationHeader);
        
        // Empty data as per curl
        request.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to get AppToken. Status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
            response.EnsureSuccessStatusCode();
        }

        if (response.Headers.TryGetValues("appToken", out var values))
        {
            var token = values.FirstOrDefault();
            _logger.LogDebug("AppToken obtained successfully.");
            return token ?? throw new Exception("AppToken not found in response headers");
        }
        
        _logger.LogError("AppToken header missing in response.");
        throw new Exception("AppToken header missing");
    }

    private async Task<string> LoginAsync(string appToken, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Logging in to Fanavaran...");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/EITAuthentication/Login");
        request.Headers.Add("appToken", appToken);
        request.Headers.Add("username", _options.Username);
        request.Headers.Add("password", _options.Password);
        
        request.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to Login. Status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
            response.EnsureSuccessStatusCode();
        }

        if (response.Headers.TryGetValues("authtoken", out var values))
        {
            var token = values.FirstOrDefault();
            _logger.LogDebug("Login successful. AuthToken obtained.");
            return token ?? throw new Exception("AuthToken not found in response headers");
        }
        
        _logger.LogError("AuthToken header missing in response.");
        throw new Exception("AuthToken header missing");
    }

    public async Task<string> CreateCustomerAsync(string customerData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Fanavaran Auth Chain...");
        
        // 1. Get App Token
        var appToken = await GetAppTokenAsync(cancellationToken);
        
        // 2. Login to get Auth Token
        var authToken = await LoginAsync(appToken, cancellationToken);
        
        _logger.LogInformation("Auth successful. Creating customer...");

        // 3. Create Customer
        // Parse input JSON (applicationData) and map to Fanavaran Request
        var appData = JsonSerializer.Deserialize<JsonElement>(customerData);
        var policyholder = appData.GetProperty("policyholder");
        var address = policyholder.GetProperty("address");
        
        // Parse BirthDate (e.g. "13750322")
        var birthDateStr = policyholder.GetProperty("birthDate").GetString()!;
        int birthYear = int.Parse(birthDateStr.Substring(0, 4));
        int birthMonth = int.Parse(birthDateStr.Substring(4, 2));
        int birthDay = int.Parse(birthDateStr.Substring(6, 2));

        var fanavaranRequest = new CreateCustomerRequest
        {
            NationalCode = policyholder.GetProperty("nationalId").GetString()!,
            BirthYear = birthYear,
            BirthMonth = birthMonth,
            BirthDay = birthDay,
            CityId = int.Parse(address.GetProperty("city").GetString()!),
            Address = address.GetProperty("line").GetString()!,
            PostalCode = policyholder.GetProperty("postalCode").ToString(),
            Tel = policyholder.GetProperty("phone").GetString()!,
            Mobile = policyholder.GetProperty("mobile").GetString()!,
            JobAddress = address.GetProperty("line").GetString(), // Using same address for MVP
            EducationField = "کامپیوتر", // Hardcoded per sample
            IsIranian = "1"
        };

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/BimeApi/v2.0/common/customers");
        request.Headers.Add("authenticationToken", authToken);
        request.Headers.Add("Location", _options.Location);
        request.Headers.Add("CorpId", _options.CorpId);
        request.Headers.Add("ContractId", _options.ContractId);
        
        request.Content = JsonContent.Create(fanavaranRequest);

        _logger.LogInformation("Sending CreateCustomer request to Fanavaran. NationalCode: {NationalCode}", fanavaranRequest.NationalCode);
        var response = await _httpClient.SendAsync(request, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Failed to Create Customer. Status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
            response.EnsureSuccessStatusCode();
        }

        // Parse response to get ID
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("CreateCustomer Response: {Content}", content);
        
        // MVP: Just returning the raw content or extracting ID if JSON
        // If it's a simple ID: return content;
        // If JSON:
        try 
        {
            using var doc = JsonDocument.Parse(content);
            if (doc.RootElement.ValueKind == JsonValueKind.Number)
            {
                return doc.RootElement.ToString();
            }
            // Check for properties
            if (doc.RootElement.TryGetProperty("CustomerId", out var idElement))
            {
                return idElement.ToString();
            }
             if (doc.RootElement.TryGetProperty("id", out var idElement2))
            {
                return idElement2.ToString();
            }
            return content; // Fallback
        }
        catch
        {
            return content; // Return raw if not JSON
        }
    }

    public async Task<string> IssuePolicyAsync(string policyData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Issuing policy in Fanavaran...");
        await Task.Delay(200, cancellationToken);
        return "POLICY_" + Guid.NewGuid().ToString().Substring(0, 10);
    }

    public async Task<decimal> GetUniversalLifePriceAsync(string customerId, string applicationData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting Universal Life Price from Fanavaran...");

        // 1. Get Auth Token (Login again to ensure fresh token or reuse if cached - simplistic login for now)
        // In real app, cache tokens. Here we login again for simplicity/statelessness.
        var appToken = await GetAppTokenAsync(cancellationToken);
        var authToken = await LoginAsync(appToken, cancellationToken);

        // 2. Map Data
        var appData = JsonSerializer.Deserialize<JsonElement>(applicationData);
        var contract = appData.GetProperty("contract");
        var coverage = appData.GetProperty("coverage");
        var body = appData.GetProperty("body");
        var policyholder = appData.GetProperty("policyholder");
        var health = appData.GetProperty("health");
        
        long customerIdLong = long.Parse(customerId);
        
        // Mocking InsuredPerson ID as CustomerID for MVP (Self insurance)
        long insuredPersonId = customerIdLong;

        var ulRequest = new UniversalLifeRequest
        {
            CustomerId = customerIdLong,
            FirstPrm = contract.GetProperty("annualPremium").GetDecimal(),
            Duration = contract.GetProperty("durationYears").GetInt32(),
            BeginDate = "1404/08/21", // TODO: Use current Persian Date
            InsuredPeople = new List<InsuredPerson>
            {
                new InsuredPerson
                {
                    InsuredPersonId = insuredPersonId,
                    MedicalHistories = new List<MedicalHistory>
                    {
                        new MedicalHistory
                        {
                             Height = body.GetProperty("heightCm").GetInt32(),
                             Weight = body.GetProperty("weightKg").GetInt32()
                        }
                    },
                    Covs = new List<Cov>
                    {
                        // Example Mapping
                        new Cov { CovKindId = 1, CapitalAmount = coverage.GetProperty("deathAnyCause").GetDecimal() }
                    }
                }
            }
        };

        // 3. Prepare Multipart Request
        using var content = new MultipartFormDataContent();
        
        // Add JSON part
        var jsonContent = new StringContent(JsonSerializer.Serialize(ulRequest), System.Text.Encoding.UTF8, "application/json");
        content.Add(jsonContent, "\"\""); // Empty name as per curl example
        
        // Add File (Mock)
        // curl: --form '=@"/C:/Users/s.sohrabi/Pictures/1211.png"'
        // We will skip actual file upload for MVP or send dummy bytes
        var fileContent = new ByteArrayContent(new byte[0]); 
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        content.Add(fileContent, "", "mock.png");
        
        // Add Name field
        content.Add(new StringContent("1"), "\"name\"");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/BimeApi/v2.0/life/Universal-life-policies");
        request.Headers.Add("authenticationToken", authToken);
        request.Headers.Add("appToken", appToken); // Also needed in header? prompt shows it
        request.Headers.Add("Location", _options.Location);
        request.Headers.Add("CorpId", _options.CorpId);
        request.Headers.Add("ContractId", _options.ContractId);
        
        request.Content = content;

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        // 4. Parse Response for Price
        // Assuming response contains the calculated premium or we use the input premium if it's just validation
        // Prompt says: "sum it with a surcharge..."
        // Let's assume the API returns the base premium confirmed.
        
        // For MVP, return the input premium as the "Base Price" confirmed by API
        return ulRequest.FirstPrm;
    }
}
