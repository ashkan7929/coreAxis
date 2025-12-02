using System.Text;
using System.Text.Json;
using System.Linq;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Services;

/// <summary>
/// Implementation of Megfa SMS service for sending SMS messages using RestSharp
/// </summary>
public class MegfaSmsService : IMegfaSmsService
{
    private readonly RestClient _restClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MegfaSmsService> _logger;
    private readonly string _from;
    // Added for richer logging
    private readonly string _username;
    private readonly string _domain;
    private readonly bool _enableSensitiveLogging;

    public MegfaSmsService(
        IConfiguration configuration,
        ILogger<MegfaSmsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var baseUrl = _configuration["MAGFA_BASE_URL"] ?? _configuration["Magfa:BaseUrl"] ?? throw new InvalidOperationException("Magfa BaseUrl is not configured (MAGFA_BASE_URL)");
        var username = _configuration["MAGFA_USERNAME"] ?? _configuration["Magfa:Username"] ?? throw new InvalidOperationException("Magfa username is not configured (MAGFA_USERNAME)");
        var password = _configuration["MAGFA_PASSWORD"] ?? _configuration["Magfa:Password"] ?? throw new InvalidOperationException("Magfa password is not configured (MAGFA_PASSWORD)");
        var domain = _configuration["MAGFA_DOMAIN"] ?? _configuration["Magfa:Domain"] ?? "magfa";
        _from = _configuration["MAGFA_FROM"] ?? _configuration["Magfa:From"] ?? throw new InvalidOperationException("Magfa sender number is not configured (MAGFA_FROM)");
        
        // assign to fields for structured logging
        _username = username;
        _domain = domain;
        _enableSensitiveLogging = bool.TryParse(_configuration["MAGFA_ENABLE_SENSITIVE_LOGGING"] ?? _configuration["Magfa:EnableSensitiveLogging"], out var flag) && flag;
        
        var options = new RestClientOptions(baseUrl)
        {
            Authenticator = new HttpBasicAuthenticator($"{username}/{domain}", password),
            ThrowOnAnyError = false
        };
        
        _restClient = new RestClient(options);
    }

    /// <inheritdoc/>
    public async Task<Result<SmsResult>> SendOtpAsync(
        string phoneNumber, 
        string otpCode, 
        CancellationToken cancellationToken = default)
    {
        var message = $"کد تایید شما: {otpCode}\nاین کد تا 5 دقیقه معتبر است.";
        return await SendSmsAsync(phoneNumber, message, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<SmsResult>> SendSmsAsync(
        string phoneNumber, 
        string message, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending SMS to {PhoneNumber}", phoneNumber);

            // Adjust resource based on BaseUrl to avoid double "send"
            string resource = "send";
            if (_restClient.Options.BaseUrl?.AbsoluteUri.TrimEnd('/').EndsWith("send", StringComparison.OrdinalIgnoreCase) == true)
            {
                resource = "";
            }

            var request = new RestRequest(resource, Method.Post);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("accept", "application/json");
            
            // Prepare body
            var requestBody = new
            {
                senders = new[] { _from },
                recipients = new[] { phoneNumber },
                messages = new[] { message }
            };
            request.AddJsonBody(requestBody);

            // Log request details (password redacted always)
            string urlForLog;
            try
            {
                urlForLog = _restClient.BuildUri(request).ToString();
            }
            catch
            {
                // Fallback if BuildUri fails (e.g. if BaseUrl is relative or malformed)
                urlForLog = $"{_restClient.Options.BaseUrl?.ToString().TrimEnd('/')}/{request.Resource}";
            }
            
            var sanitizedMessage = message;
            var logBodyObject = new
            {
                senders = new[] { _from },
                recipients = new[] { phoneNumber },
                messages = new[] { sanitizedMessage }
            };
            string requestJsonForLog = JsonSerializer.Serialize(logBodyObject, new JsonSerializerOptions { WriteIndented = false });
            string requestHeadersForLog = "accept=application/json; cache-control=no-cache";

            _logger.LogInformation(
                "Magfa SMS Request => Method: POST, Url: {Url}, Auth: {Username}/{Domain}, Password: {Password}, Headers: {Headers}, Body: {Body}",
                urlForLog,
                _username,
                _domain,
                "[REDACTED]",
                requestHeadersForLog,
                requestJsonForLog);

            var response = await _restClient.ExecuteAsync(request, cancellationToken);
            var responseContent = response.Content ?? string.Empty;

            // Log response details
            var headerPairs = response.Headers?.Select(h => $"{h.Name}={h.Value}")?.ToArray() ?? Array.Empty<string>();
            _logger.LogInformation(
                "Magfa SMS Response <= StatusCode: {StatusCode}, IsSuccessful: {IsSuccessful}, ContentType: {ContentType}, Headers: {Headers}, Content: {Content}",
                response.StatusCode,
                response.IsSuccessful,
                response.ContentType,
                string.Join("; ", headerPairs),
                responseContent);

            if (!response.IsSuccessful)
            {
                _logger.LogError("Magfa SMS API returned error status {StatusCode}: {Content}", 
                    response.StatusCode, responseContent);
                
                var errorResult = new SmsResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"SMS service returned error: {response.StatusCode} - {response.ErrorMessage}",
                    MessageId = null
                };
                
                return Result<SmsResult>.Success(errorResult);
            }

            var apiResponse = JsonSerializer.Deserialize<MagfaApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (apiResponse == null)
            {
                _logger.LogError("Failed to deserialize Magfa SMS response: {Content}", responseContent);
                
                var errorResult = new SmsResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid response from SMS service",
                    MessageId = null
                };
                
                return Result<SmsResult>.Success(errorResult);
            }

            var smsResult = new SmsResult
            {
                IsSuccess = false,
                ErrorMessage = null,
                MessageId = null
            };

            if (apiResponse.Status == 0 && apiResponse.Messages?.Any() == true)
            {
                var firstMessage = apiResponse.Messages.First();
                smsResult.IsSuccess = firstMessage.Status == 0;
                smsResult.MessageId = firstMessage.Id?.ToString();
                smsResult.ErrorMessage = firstMessage.Status != 0 ? $"Message failed with status: {firstMessage.Status}" : null;
            }
            else
            {
                smsResult.ErrorMessage = apiResponse.Status != 0 ? $"API error status: {apiResponse.Status}" : "No messages returned";
            }

            if (smsResult.IsSuccess)
            {
                _logger.LogInformation("SMS sent successfully to {PhoneNumber}. MessageId: {MessageId}", 
                    phoneNumber, smsResult.MessageId);
            }
            else
            {
                _logger.LogWarning("SMS sending failed for {PhoneNumber}: {ErrorMessage}", 
                    phoneNumber, smsResult.ErrorMessage);
            }

            return Result<SmsResult>.Success(smsResult);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while calling Megfa SMS service");
            
            var errorResult = new SmsResult
            {
                IsSuccess = false,
                ErrorMessage = "Network error occurred while sending SMS",
                MessageId = null
            };
            
            return Result<SmsResult>.Success(errorResult);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while calling Megfa SMS service");
            
            var errorResult = new SmsResult
            {
                IsSuccess = false,
                ErrorMessage = "Timeout occurred while sending SMS",
                MessageId = null
            };
            
            return Result<SmsResult>.Success(errorResult);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error occurred while processing Megfa SMS response");
            
            var errorResult = new SmsResult
            {
                IsSuccess = false,
                ErrorMessage = "Invalid response format from SMS service",
                MessageId = null
            };
            
            return Result<SmsResult>.Success(errorResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while calling Megfa SMS service");
            
            var errorResult = new SmsResult
            {
                IsSuccess = false,
                ErrorMessage = "An unexpected error occurred while sending SMS",
                MessageId = null
            };
            
            return Result<SmsResult>.Success(errorResult);
        }
    }

    // ===== Helpers for safe logging =====
    private static string SanitizeMessage(string message)
    {
        if (string.IsNullOrEmpty(message)) return message;
        // Mask continuous digit sequences (like OTP codes) to avoid leaking secrets in logs
        var chars = message.ToCharArray();
        int run = 0;
        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsDigit(chars[i]))
            {
                run++;
                if (run >= 4) chars[i] = '*';
            }
            else
            {
                run = 0;
            }
        }
        return new string(chars);
    }
}

/// <summary>
/// Internal class for deserializing Magfa API response
/// </summary>
internal class MagfaApiResponse
{
    public int Status { get; set; }
    public List<MagfaMessage>? Messages { get; set; }
}

internal class MagfaMessage
{
    public int Status { get; set; }
    public long? Id { get; set; }
    public long? UserId { get; set; }
    public int? Parts { get; set; }
    public decimal? Tariff { get; set; }
    public string? Alphabet { get; set; }
    public string? Recipient { get; set; }
}