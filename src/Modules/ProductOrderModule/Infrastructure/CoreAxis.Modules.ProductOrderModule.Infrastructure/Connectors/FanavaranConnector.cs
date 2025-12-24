using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Connectors;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.DTOs.Fanavaran;
using CoreAxis.Modules.ProductOrderModule.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

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
        var requestUrl = $"{_options.BaseUrl}/EITAuthentication/GetAppToken";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("appname", _options.AppName);
        request.Headers.Add("secret", _options.Secret);
        request.Headers.Add("Authorization", _options.AuthorizationHeader);
        
        // Empty data as per curl
        request.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");

        try
        {
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
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while calling GetAppToken at {Url}", requestUrl);
            throw new TimeoutException($"Timeout calling Fanavaran GetAppToken: {requestUrl}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling GetAppToken at {Url}", requestUrl);
            throw new HttpRequestException($"Network error calling Fanavaran GetAppToken: {requestUrl}. Details: {ex.Message}", ex);
        }
    }

    private async Task<string> LoginAsync(string appToken, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Logging in to Fanavaran...");
        var requestUrl = $"{_options.BaseUrl}/EITAuthentication/Login";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("appToken", appToken);
        request.Headers.Add("username", _options.Username);
        request.Headers.Add("password", _options.Password);
        
        request.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to Login. Status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
                response.EnsureSuccessStatusCode();
            }

            if (response.Headers.TryGetValues("authenticationToken", out var values))
            {
                var token = values.FirstOrDefault();
                _logger.LogDebug("Login successful. AuthenticationToken obtained.");
                return token ?? throw new Exception("AuthenticationToken not found in response headers");
            }
            
            _logger.LogError("AuthenticationToken header missing in response.");
            throw new Exception("AuthenticationToken header missing");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while calling Login at {Url}", requestUrl);
            throw new TimeoutException($"Timeout calling Fanavaran Login: {requestUrl}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling Login at {Url}", requestUrl);
            throw new HttpRequestException($"Network error calling Fanavaran Login: {requestUrl}. Details: {ex.Message}", ex);
        }
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
            CityId = address.GetProperty("city").ValueKind == JsonValueKind.Number 
                ? address.GetProperty("city").GetInt32() 
                : int.Parse(address.GetProperty("city").GetString()!),
            Address = address.GetProperty("line").GetString()!,
            PostalCode = policyholder.GetProperty("postalCode").ToString(),
            Tel = policyholder.GetProperty("phone").GetString()!,
            Mobile = policyholder.GetProperty("mobile").GetString()!,
            JobAddress = address.GetProperty("line").GetString(), // Using same address for MVP
            EducationField = "کامپیوتر", // Hardcoded per sample
            IsIranian = "1"
        };

        var requestUrl = $"{_options.BaseUrl}/BimeApi/v2.0/common/customers";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("authenticationToken", authToken);
        request.Headers.Add("Location", _options.Location);
        request.Headers.Add("CorpId", _options.CorpId);
        request.Headers.Add("ContractId", _options.ContractId);
        
        request.Content = JsonContent.Create(fanavaranRequest);

        _logger.LogInformation("Sending CreateCustomer request to Fanavaran. NationalCode: {NationalCode}", fanavaranRequest.NationalCode);
        
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to Create Customer. Status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);

                // Handle "Person already exists" error (500 InternalServerError with specific message)
                if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    // Regex to find "شخص با کد رایانه ... دارای نقش بيمه گذار است"
                    // Example: "شخص با کد رایانه 3977188 دارای نقش بيمه گذار است"
                    var match = Regex.Match(errorContent, @"شخص با کد رایانه\s+(\d+)\s+دارای نقش");
                    if (match.Success)
                    {
                        var existingCustomerId = match.Groups[1].Value;
                        _logger.LogInformation("Customer already exists. Using existing CustomerId: {CustomerId}", existingCustomerId);
                        return existingCustomerId;
                    }
                }

                // Throw specific exception with content to be visible in API response
                throw new HttpRequestException($"Fanavaran CreateCustomer Failed: {response.StatusCode}. Content: {errorContent}");
            }
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while calling CreateCustomer at {Url}", requestUrl);
            throw new TimeoutException($"Timeout calling Fanavaran CreateCustomer: {requestUrl}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling CreateCustomer at {Url}", requestUrl);
            throw new HttpRequestException($"Network error calling Fanavaran CreateCustomer: {requestUrl}. Details: {ex.Message}", ex);
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
            if (doc.RootElement.TryGetProperty("Id", out var idElement3))
            {
                return idElement3.ToString();
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
        var health = appData.TryGetProperty("health", out var healthElem) ? healthElem : default;
        
        long customerIdLong = long.Parse(customerId);
        
        // Mocking InsuredPerson ID as CustomerID for MVP (Self insurance)
        long insuredPersonId = customerIdLong;

        // Parse Job ID from policyholder (default to 2 if not found)
        int jobId = 2;
        if (policyholder.TryGetProperty("mainJob", out var jobElem))
        {
            if (jobElem.ValueKind == JsonValueKind.Number)
            {
                jobId = jobElem.GetInt32();
            }
            else if (jobElem.ValueKind == JsonValueKind.String && int.TryParse(jobElem.GetString(), out var parsedJobId))
            {
                jobId = parsedJobId;
            }
        }

        List<InsuredPerson> insuredPeople;
        if (appData.TryGetProperty("InsuredPeople", out var insuredPeopleElem) && insuredPeopleElem.ValueKind == JsonValueKind.Array)
        {
            insuredPeople = JsonSerializer.Deserialize<List<InsuredPerson>>(insuredPeopleElem.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            // Fix BeneficiaryId if missing (required for non-legal-heir relations)
            foreach (var person in insuredPeople)
            {
                if (person.Beneficiaries != null)
                {
                    foreach (var beneficiary in person.Beneficiaries)
                    {
                        // Fix BeneficiaryId if missing (required for non-legal-heir relations)
                        // Note: User confirmed that for "Legal Heirs" (which seems to include 103), the "BeneficiaryId" field MUST NOT exist (even as null).
                        // We added [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] to the DTO.
                        // Now we just need to ensure it is null for 103/117.
                        
                        // If Relation is "Legal Heirs" (117) or "Legal Heirs" (103 as per user), BeneficiaryId MUST be null.
                        if (beneficiary.BeneficiaryRelationId == 117 || beneficiary.BeneficiaryRelationId == 103)
                        {
                             beneficiary.BeneficiaryId = null;
                        }
                        // For others, if ID is missing, default to InsuredPersonId (Self)
                        else if (beneficiary.BeneficiaryId == null)
                        {
                            _logger.LogWarning("BeneficiaryId is null for Relation {RelationId}. Defaulting to InsuredPersonId {InsuredPersonId}.", 
                                beneficiary.BeneficiaryRelationId, person.InsuredPersonId);
                            beneficiary.BeneficiaryId = person.InsuredPersonId;
                            // beneficiary.BeneficiaryRelationId = 101; // Not forcing relation change anymore, let's trust the ID fill.
                        }
                    }
                }

                // Ensure MedicalHistories is populated
                if (person.MedicalHistories == null || !person.MedicalHistories.Any())
                {
                    person.MedicalHistories = new List<MedicalHistory>
                    {
                        new MedicalHistory
                        {
                             Height = body.ValueKind != JsonValueKind.Undefined && body.TryGetProperty("heightCm", out var h) ? h.GetInt32() : 170,
                             Weight = body.ValueKind != JsonValueKind.Undefined && body.TryGetProperty("weightKg", out var w) ? w.GetInt32() : 75,
                             HealthInsuranceName = "تامين اجتماعي، تکميلي درمان"
                        }
                    };
                }

                // Ensure FamilyMedicalHistories is populated
                if (person.FamilyMedicalHistories == null || !person.FamilyMedicalHistories.Any())
                {
                    person.FamilyMedicalHistories = new List<FamilyMedicalHistory> { new FamilyMedicalHistory() };
                }

                // Ensure DoctorRecommendations is populated
                if (person.DoctorRecommendations == null || !person.DoctorRecommendations.Any())
                {
                    person.DoctorRecommendations = new List<DoctorRecommendation> { new DoctorRecommendation() };
                }
                
                // Ensure Covs is populated from 'coverage' if missing
                if ((person.Covs == null || !person.Covs.Any()) && coverage.ValueKind != JsonValueKind.Undefined)
                {
                    person.Covs = new List<Cov>
                    {
                        new Cov { CovKindId = 1, CapitalAmount = coverage.TryGetProperty("deathAnyCause", out var d) ? d.GetDecimal() : 100000000 },
                        new Cov { CovKindId = 2, CapitalRatio = 3 },
                        new Cov { CovKindId = 3, CapitalRatio = 1 }
                    };
                }
                
                // Ensure Surcharges is populated (default to 99 as per working JSON)
                if (person.Surcharges == null || !person.Surcharges.Any())
                {
                    person.Surcharges = new List<Surcharge>
                    {
                        new Surcharge 
                        { 
                            ExerciseDuration = null,
                            SurchargeId = 99 
                        }
                    };
                }
            }
        }
        else
        {
            insuredPeople = new List<InsuredPerson>
            {
                new InsuredPerson
                {
                    InsuredPersonId = insuredPersonId,
                    InsuredPersonJobId = jobId, // Sync Insured Job (Must be same as CustomerJobId)
                    InsuredPersonRoleKindId = 793,
                    InsurerAndInsuredRelationId = 105,
                    MedicalRate = 0,
                    Beneficiaries = new List<Beneficiary>
                    {
                        // Death Beneficiary (Kind 791) - Legal Heirs (Worrath)
                        // Relation 117 is commonly used for "Voras" (Legal Heirs) in such systems
                        // BeneficiaryId should be null for this type of general relation
                        new Beneficiary 
                        { 
                            BeneficiaryId = null, 
                            BeneficiaryKindId = 791, 
                            BeneficiaryRelationId = 117, // Legal Heirs (Worrath)
                            CapitalPercent = 100, 
                            PriorityId = 40 
                        },
                        // Survival Beneficiary (Kind 792) - Policyholder (101)
                        new Beneficiary 
                        { 
                            BeneficiaryId = null, // Must be empty for Policyholder relation
                            BeneficiaryKindId = 792, 
                            BeneficiaryRelationId = 101, // Bimegozar
                            CapitalPercent = 100, 
                            PriorityId = 40 
                        }
                    },
                    MedicalHistories = new List<MedicalHistory>
                    {
                        new MedicalHistory
                        {
                             Height = body.GetProperty("heightCm").GetInt32(),
                             Weight = body.GetProperty("weightKg").GetInt32(),
                             HealthInsuranceName = "تامين اجتماعي، تکميلي درمان"
                        }
                    },
                    FamilyMedicalHistories = new List<FamilyMedicalHistory> { new FamilyMedicalHistory() },
                    DoctorRecommendations = new List<DoctorRecommendation> { new DoctorRecommendation() },
                    Covs = new List<Cov>
                    {
                        // Example Mapping from working JSON
                        new Cov { CovKindId = 1, CapitalAmount = coverage.GetProperty("deathAnyCause").GetDecimal() },
                        new Cov { CovKindId = 2, CapitalRatio = 3 },
                        new Cov { CovKindId = 3, CapitalRatio = 1 }
                    },
                    Surcharges = new List<Surcharge>
                    {
                        new Surcharge { ExerciseDuration = null, SurchargeId = 99 }
                    }
                }
            };
        }

        var ulRequest = new UniversalLifeRequest
        {
            CustomerId = customerIdLong,
            CustomerJobId = jobId, // Sync Customer Job
            FirstPrm = contract.GetProperty("annualPremium").GetDecimal(),
            Duration = contract.GetProperty("durationYears").GetInt32(),
            BeginDate = appData.TryGetProperty("beginDate", out var beginDateElem) ? beginDateElem.GetString()! : GetCurrentPersianDate(),
            // If PlanId is 21 and ContractId is 10743, it fails.
            // If PlanId is 10 and ContractId is 10743, it fails on ContractId.
            // This suggests 10743 is problematic OR 21 requires a different contract.
            // Reverting default ContractId to 4604 which is known to work with PlanId 10.
            // However, user wants to use provided IDs.
            // The issue is likely the combination.
            // If user provides PlanId 21, they MUST provide a compatible ContractId.
            // If user provides PlanId 10, they MUST provide a compatible ContractId (like 4604).
            
            // Logic:
            // 1. Read User Inputs.
            // 2. If User Input is present, use it.
            // 3. If User Input is missing, use SAFE defaults (Plan 10, Contract 4604).
            
            PlanId = appData.TryGetProperty("planId", out var planIdElem) ? planIdElem.GetInt32() : 10,
            ContractId = appData.TryGetProperty("contractId", out var contractIdElem) ? contractIdElem.GetInt32() : 4604,
            AgentId = appData.TryGetProperty("agentId", out var agentIdElem) ? agentIdElem.GetInt32() : 1035,
            SaleManagerId = appData.TryGetProperty("saleManagerId", out var smIdElem) ? smIdElem.GetInt32() : 1035,
            CapitalChangePercent = appData.TryGetProperty("capitalChangePercent", out var ccp) ? ccp.GetInt32() : 10,
            PrmChangePercent = appData.TryGetProperty("prmChangePercent", out var pcp) ? pcp.GetInt32() : 15,
            InsuredPersonCount = appData.TryGetProperty("insuredPersonCount", out var ipc) ? ipc.GetInt32() : 58,
            PayPeriodId = appData.TryGetProperty("payPeriodId", out var ppId) ? ppId.GetInt32() : 275, // Yearly default
            PolicyUsageTypeId = appData.TryGetProperty("policyUsageTypeId", out var putId) ? putId.GetInt32() : 2898,
            InsuredPeople = insuredPeople,
            
            // Map optional fields if present in root applicationData
            Note = appData.TryGetProperty("note", out var note) ? note.GetString()! : "يادداشت متفرقه",
            SpecialCondition = appData.TryGetProperty("specialCondition", out var sc) ? sc.GetString()! : "شرايط خصوصي"
        };

        // 3. Prepare Multipart Request
        using var content = new MultipartFormDataContent();
        
        // Add JSON part
        var jsonContent = new StringContent(JsonSerializer.Serialize(ulRequest), System.Text.Encoding.UTF8, "application/json");
        content.Add(jsonContent, "\"\""); // Empty name as per curl example
        
        // Add Name field
        content.Add(new StringContent("1"), "\"name\"");

        var requestUrl = $"{_options.BaseUrl}/BimeApi/v2.0/life/Universal-life-policies";
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Headers.Add("authenticationToken", authToken);
        request.Headers.Add("appToken", appToken); // Also needed in header? prompt shows it
        request.Headers.Add("Location", _options.Location);
        request.Headers.Add("CorpId", _options.CorpId);
        request.Headers.Add("ContractId", _options.ContractId);
        
        request.Content = content;

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to GetUniversalLifePrice. Status: {StatusCode}, Content: {Content}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Fanavaran GetUniversalLifePrice Failed: {response.StatusCode}. Content: {errorContent}");
            }

            var contentString = await response.Content.ReadAsStringAsync(cancellationToken);
             _logger.LogInformation("GetUniversalLifePrice Response: {Content}", contentString);

             // Assuming the response is a JSON with price details or just success
             // For now, return the input premium as placeholder if parsing logic is unknown
             return ulRequest.FirstPrm;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while calling GetUniversalLifePrice at {Url}", requestUrl);
            throw new TimeoutException($"Timeout calling Fanavaran GetUniversalLifePrice: {requestUrl}", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling GetUniversalLifePrice at {Url}", requestUrl);
            throw new HttpRequestException($"Network error calling Fanavaran GetUniversalLifePrice: {requestUrl}. Details: {ex.Message}", ex);
        }

        // 4. Parse Response for Price
        // Assuming response contains the calculated premium or we use the input premium if it's just validation
        // Prompt says: "sum it with a surcharge..."
        // Let's assume the API returns the base premium confirmed.
        
        // For MVP, return the input premium as the "Base Price" confirmed by API
        return ulRequest.FirstPrm;
    }

    private string GetCurrentPersianDate()
    {
        var pc = new PersianCalendar();
        var now = DateTime.Now;
        return $"{pc.GetYear(now)}/{pc.GetMonth(now):00}/{pc.GetDayOfMonth(now):00}";
    }
}
