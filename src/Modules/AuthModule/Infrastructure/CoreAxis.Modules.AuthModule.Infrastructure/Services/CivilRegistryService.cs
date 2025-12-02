using System.Text;
using System.Text.Json;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Services;

/// <summary>
/// Implementation of Civil Registry service for retrieving personal information
/// </summary>
public class CivilRegistryService : ICivilRegistryService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CivilRegistryService> _logger;
    private readonly string _baseUrl;
    private readonly string _token;

    public CivilRegistryService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CivilRegistryService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["CIVIL_REGISTRY_BASE_URL"] ?? _configuration["CivilRegistry:BaseUrl"] ?? throw new InvalidOperationException("Civil Registry BaseUrl is not configured (CIVIL_REGISTRY_BASE_URL)");
        _token = _configuration["CIVIL_REGISTRY_TOKEN"] ?? _configuration["CivilRegistry:Token"] ?? throw new InvalidOperationException("Civil Registry Token is not configured (CIVIL_REGISTRY_TOKEN)");
    }

    /// <inheritdoc/>
    public async Task<Result<CivilRegistryPersonalInfo>> GetPersonalInfoAsync(
        string nationalCode, 
        string birthDate, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Civil Registry lookup for national code: {NationalCode}", nationalCode);
            _logger.LogInformation("Civil Registry BaseUrl: {BaseUrl}", _baseUrl);
            _logger.LogInformation("Civil Registry Token configured: {TokenConfigured}", !string.IsNullOrEmpty(_token));
            _logger.LogInformation("Civil Registry BirthDate: {BirthDate}", birthDate);

            var requestData = new
            {
                nationalCode = nationalCode,
                birthDate = birthDate
            };

            var json = JsonSerializer.Serialize(requestData);
            _logger.LogInformation("Civil Registry Request JSON: {RequestJson}", json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("token", _token);
            _logger.LogInformation("Civil Registry Full URL: {FullUrl}", _baseUrl);

            var response = await _httpClient.PostAsync(
                $"{_baseUrl}", 
                content, 
                cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Civil Registry API Response Content: {ResponseContent}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Civil Registry API returned error status {StatusCode}: {Content}", 
                    response.StatusCode, responseContent);
                return Result<CivilRegistryPersonalInfo>.Failure(
                    $"Civil Registry service returned error: {response.StatusCode}");
            }

            var apiResponse = JsonSerializer.Deserialize<CivilRegistryResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse?.Data == null)
            {
                _logger.LogError("Failed to deserialize Civil Registry response: {Content}", responseContent);
                return Result<CivilRegistryPersonalInfo>.Failure("Invalid response from Civil Registry service");
            }

            if (!apiResponse.IsSuccess)
            {
                _logger.LogWarning("Civil Registry returned unsuccessful response: {Message}", apiResponse.Message);
                return Result<CivilRegistryPersonalInfo>.Failure(
                    apiResponse.Message ?? "Civil Registry lookup was not successful");
            }

            if (apiResponse.Data.Result == null)
            {
                _logger.LogError("Civil Registry response missing result data");
                return Result<CivilRegistryPersonalInfo>.Failure("No personal information found");
            }

            var personalInfo = new CivilRegistryPersonalInfo
            {
                FirstName = apiResponse.Data.Result.FirstName,
                LastName = apiResponse.Data.Result.LastName,
                FatherName = apiResponse.Data.Result.FatherName,
                CertNumber = apiResponse.Data.Result.CertNumber,
                Gender = apiResponse.Data.Result.Gender,
                Aliveness = apiResponse.Data.Result.Aliveness,
                IdentificationSerial = apiResponse.Data.Result.IdentificationSerial,
                IdentificationSeri = apiResponse.Data.Result.IdentificationSeri,
                OfficeName = apiResponse.Data.Result.OfficeName,
                TrackId = apiResponse.Data.TrackId
            };

            _logger.LogInformation("Civil Registry lookup completed successfully. TrackId: {TrackId}", 
                personalInfo.TrackId);

            return Result<CivilRegistryPersonalInfo>.Success(personalInfo);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while calling Civil Registry service. BaseUrl: {BaseUrl}, InnerException: {InnerException}, ExceptionType: {ExceptionType}", 
                _baseUrl, ex.InnerException?.Message, ex.GetType().Name);
            return Result<CivilRegistryPersonalInfo>.Failure("Network error occurred while retrieving personal information");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while calling Civil Registry service. BaseUrl: {BaseUrl}, InnerException: {InnerException}, ExceptionType: {ExceptionType}", 
                _baseUrl, ex.InnerException?.Message, ex.GetType().Name);
            return Result<CivilRegistryPersonalInfo>.Failure("Timeout occurred while retrieving personal information");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error occurred while processing Civil Registry response. BaseUrl: {BaseUrl}, InnerException: {InnerException}, ExceptionType: {ExceptionType}", 
                _baseUrl, ex.InnerException?.Message, ex.GetType().Name);
            return Result<CivilRegistryPersonalInfo>.Failure("Invalid response format from Civil Registry service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while calling Civil Registry service. BaseUrl: {BaseUrl}, InnerException: {InnerException}, ExceptionType: {ExceptionType}", 
                _baseUrl, ex.InnerException?.Message, ex.GetType().Name);
            return Result<CivilRegistryPersonalInfo>.Failure("An unexpected error occurred while retrieving personal information");
        }
    }
}