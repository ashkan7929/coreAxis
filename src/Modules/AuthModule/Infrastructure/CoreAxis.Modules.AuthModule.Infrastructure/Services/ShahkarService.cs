using System.Text;
using System.Text.Json;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Services;

/// <summary>
/// Implementation of Shahkar service for national code and mobile number verification
/// </summary>
public class ShahkarService : IShahkarService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ShahkarService> _logger;
    private readonly string _baseUrl;
    private readonly string _token;

    public ShahkarService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ShahkarService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["SHAHKAR_BASE_URL"] ?? _configuration["Shahkar:BaseUrl"] ?? throw new InvalidOperationException("Shahkar BaseUrl is not configured (SHAHKAR_BASE_URL)");
        _token = _configuration["SHAHKAR_TOKEN"] ?? _configuration["Shahkar:Token"] ?? throw new InvalidOperationException("Shahkar token is not configured (SHAHKAR_TOKEN)");
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> VerifyNationalCodeAndMobileAsync(
        string nationalCode,
        string mobileNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Shahkar verification for national code: {NationalCode}", nationalCode);
            _logger.LogInformation("Shahkar BaseUrl: {BaseUrl}", _baseUrl);
            _logger.LogInformation("Shahkar Token configured: {HasToken}", !string.IsNullOrEmpty(_token));
            _logger.LogInformation("Mobile number: {MobileNumber}", mobileNumber);

            var requestData = new
            {
                nationalCode = nationalCode,
                mobileNumber = mobileNumber
            };

            var json = JsonSerializer.Serialize(requestData);
            _logger.LogInformation("Request JSON: {RequestJson}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("token", _token);

            var fullUrl = _baseUrl;
            _logger.LogInformation("Making request to: {FullUrl}", fullUrl);

            var response = await _httpClient.PostAsync(
                fullUrl,
                content,
                cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("Shahkar API Response Content: {ResponseContent}", response);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Shahkar API returned error status {StatusCode}: {Content}",
                    response.StatusCode, responseContent);
                return Result<bool>.Failure(
                    $"Shahkar service returned error: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<ShahkarApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                _logger.LogError("Failed to deserialize Shahkar response: {Content}", responseContent);
                return Result<bool>.Failure("Invalid response from Shahkar service");
            }

            var isVerified = result.IsSuccess && result.Data?.Result?.IsMatched == true;
            var trackId = result.Data?.TrackId ?? string.Empty;

            _logger.LogInformation("Shahkar verification completed. IsVerified: {IsVerified}, TrackId: {TrackId}",
                isVerified, trackId);

            return Result<bool>.Success(isVerified);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while calling Shahkar service. BaseUrl: {BaseUrl}, InnerException: {InnerException}",
                _baseUrl, ex.InnerException?.Message);
            return Result<bool>.Failure($"Network error occurred while verifying with Shahkar: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while calling Shahkar service. BaseUrl: {BaseUrl}", _baseUrl);
            return Result<bool>.Failure("Timeout occurred while verifying with Shahkar");
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error occurred while processing Shahkar response. BaseUrl: {BaseUrl}", _baseUrl);
            return Result<bool>.Failure("Invalid response format from Shahkar service");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while calling Shahkar service. BaseUrl: {BaseUrl}, ExceptionType: {ExceptionType}",
                _baseUrl, ex.GetType().Name);
            return Result<bool>.Failure($"An unexpected error occurred while verifying with Shahkar: {ex.Message}");
        }
    }
}

/// <summary>
/// Internal class for deserializing Shahkar API response
/// </summary>
internal class ShahkarApiResponse
{
    public ShahkarApiData? Data { get; set; }
    public string? Message { get; set; }
    public bool IsSuccess { get; set; }
}

/// <summary>
/// Internal class for Shahkar API data
/// </summary>
internal class ShahkarApiData
{
    public ShahkarResult? Result { get; set; }
    public string? TrackId { get; set; }
}

/// <summary>
/// Internal class for Shahkar result
/// </summary>
internal class ShahkarResult
{
    public bool IsMatched { get; set; }
}