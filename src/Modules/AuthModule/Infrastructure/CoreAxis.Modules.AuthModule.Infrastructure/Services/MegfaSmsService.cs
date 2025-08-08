using System.Text;
using System.Text.Json;
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

    public MegfaSmsService(
        IConfiguration configuration,
        ILogger<MegfaSmsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var baseUrl = _configuration["Magfa:BaseUrl"] ?? "https://sms.magfa.com/api/http/sms/v2";
        var username = _configuration["Magfa:Username"] ?? throw new InvalidOperationException("Magfa username is not configured");
        var password = _configuration["Magfa:Password"] ?? throw new InvalidOperationException("Magfa password is not configured");
        var domain = _configuration["Magfa:Domain"] ?? "magfa";
        _from = _configuration["Magfa:From"] ?? throw new InvalidOperationException("Magfa sender number is not configured");
        
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

            var request = new RestRequest("send", Method.Post);
            request.AddHeader("cache-control", "no-cache");
            request.AddHeader("accept", "application/json");
            
            request.AddJsonBody(new
            {
                senders = new[] { _from },
                recipients = new[] { phoneNumber },
                messages = new[] { message }
            });

            var response = await _restClient.ExecuteAsync(request, cancellationToken);
            var responseContent = response.Content ?? string.Empty;

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