using System.Security.Cryptography;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Services;

/// <summary>
/// Implementation of OTP service for generating and validating OTP codes
/// </summary>
public class OtpService : IOtpService
{
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpService> _logger;
    private readonly int _otpLength;
    private readonly int _otpExpiryMinutes;
    private readonly int _maxAttempts;

    public OtpService(
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<OtpService> logger)
    {
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _otpLength = _configuration.GetValue<int>("Otp:Length", 6);
        _otpExpiryMinutes = _configuration.GetValue<int>("Otp:ExpiryMinutes", 5);
        _maxAttempts = _configuration.GetValue<int>("Otp:MaxAttempts", 3);
    }

    /// <inheritdoc/>
    public async Task<Result<string>> GenerateOtpAsync(
        string mobileNumber, 
        OtpPurpose purpose, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(mobileNumber, purpose);
            
            // Check if there's an existing OTP that's still valid
            if (_cache.TryGetValue(cacheKey, out OtpCode? existingOtp) && existingOtp != null)
            {
                if (existingOtp.ExpiresAt > DateTime.UtcNow)
                {
                    _logger.LogInformation("Returning existing valid OTP for {MobileNumber} with purpose {Purpose}", 
                        mobileNumber, purpose);
                    return Result<string>.Success(existingOtp.Code);
                }
                
                // Remove expired OTP
                _cache.Remove(cacheKey);
            }

            // Generate new OTP code
            var code = GenerateRandomCode(_otpLength);
            var expiresAt = DateTime.UtcNow.AddMinutes(_otpExpiryMinutes);
            
            var otpCode = new OtpCode
            {
                Code = code,
                MobileNumber = mobileNumber,
                Purpose = purpose,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            // Store in cache with expiration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiresAt,
                Priority = CacheItemPriority.Normal
            };
            
            _cache.Set(cacheKey, otpCode, cacheOptions);
            
            _logger.LogInformation("Generated new OTP for {MobileNumber} with purpose {Purpose}. Expires at {ExpiresAt}", 
                mobileNumber, purpose, expiresAt);

            return Result<string>.Success(code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating OTP for {MobileNumber} with purpose {Purpose}", 
                mobileNumber, purpose);
            return Result<string>.Failure("Failed to generate OTP code");
        }
    }

    /// <inheritdoc/>
    public async Task<Result<bool>> VerifyOtpAsync(
        string mobileNumber, 
        string otpCode, 
        OtpPurpose purpose, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(mobileNumber, purpose);
            OtpCode? storedOtp = null;
            
            // First try to find OTP with the exact purpose
            if (!_cache.TryGetValue(cacheKey, out storedOtp) || storedOtp == null)
            {
                // If not found and purpose is Login, try to find Registration OTP
                if (purpose == OtpPurpose.Login)
                {
                    var registrationCacheKey = GetCacheKey(mobileNumber, OtpPurpose.Registration);
                    if (!_cache.TryGetValue(registrationCacheKey, out storedOtp) || storedOtp == null)
                    {
                        _logger.LogWarning("OTP verification failed: No OTP found for {MobileNumber} with purpose {Purpose}", 
                            mobileNumber, purpose);
                        return Result<bool>.Success(false);
                    }
                }
                else
                {
                    _logger.LogWarning("OTP verification failed: No OTP found for {MobileNumber} with purpose {Purpose}", 
                        mobileNumber, purpose);
                    return Result<bool>.Success(false);
                }
            }

            // Check if OTP is expired
            if (storedOtp.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("OTP verification failed: OTP expired for {MobileNumber} with purpose {Purpose}", 
                    mobileNumber, purpose);
                _cache.Remove(cacheKey);
                return Result<bool>.Success(false);
            }

            // Verify the code
            if (storedOtp.Code != otpCode)
            {
                _logger.LogWarning("OTP verification failed: Invalid code for {MobileNumber} with purpose {Purpose}", 
                    mobileNumber, purpose);
                return Result<bool>.Success(false);
            }

            // Remove the OTP after successful verification only for certain purposes
            // For Registration and Login, keep the OTP for a short time to allow multiple verifications
            if (purpose == OtpPurpose.PasswordReset || purpose == OtpPurpose.PhoneVerification)
            {
                _cache.Remove(cacheKey);
            }
            else
            {
                // For Registration and Login, mark as used but keep for 2 more minutes
                storedOtp.IsUsed = true;
                var shortExpiryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.UtcNow.AddMinutes(2),
                    Priority = CacheItemPriority.Normal
                };
                _cache.Set(cacheKey, storedOtp, shortExpiryOptions);
            }
            
            _logger.LogInformation("OTP verification successful for {MobileNumber} with purpose {Purpose}", 
                mobileNumber, purpose);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while verifying OTP for {MobileNumber} with purpose {Purpose}", 
                mobileNumber, purpose);
            return Result<bool>.Failure("Failed to verify OTP code");
        }
    }

    /// <inheritdoc/>
    public async Task InvalidateOtpAsync(
        string mobileNumber, 
        OtpPurpose purpose, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GetCacheKey(mobileNumber, purpose);
            _cache.Remove(cacheKey);
            
            _logger.LogInformation("OTP invalidated for {MobileNumber} with purpose {Purpose}", 
                mobileNumber, purpose);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while invalidating OTP for {MobileNumber} with purpose {Purpose}", 
                mobileNumber, purpose);
            throw;
        }
    }

    /// <summary>
    /// Generates a cache key for the OTP
    /// </summary>
    private static string GetCacheKey(string mobileNumber, OtpPurpose purpose)
    {
        return $"otp:{purpose}:{mobileNumber}";
    }

    /// <summary>
    /// Generates a random numeric code of specified length
    /// </summary>
    private static string GenerateRandomCode(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        var code = string.Empty;
        
        for (int i = 0; i < length; i++)
        {
            rng.GetBytes(bytes);
            var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 10;
            code += randomNumber.ToString();
        }
        
        return code;
    }
}