using System.Security.Cryptography;
using CoreAxis.Modules.AuthModule.Application.Services;
using CoreAxis.Modules.AuthModule.Domain.Enums;
using CoreAxis.Modules.AuthModule.Domain.Entities;
using CoreAxis.Modules.AuthModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace CoreAxis.Modules.AuthModule.Infrastructure.Services;

/// <summary>
/// Implementation of OTP service for generating and validating OTP codes
/// </summary>
public class OtpService : IOtpService
{
    private readonly IOtpCodeRepository _otpCodeRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly int _otpLength;
    private readonly int _otpExpiryMinutes;
    private readonly int _maxAttempts;
    private readonly int _maxOtpPerHour;

    public OtpService(
        IOtpCodeRepository otpCodeRepository,
        IConfiguration configuration,
        ILogger<OtpService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _otpCodeRepository = otpCodeRepository;
        _configuration = configuration;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _otpLength = _configuration.GetValue<int>("Otp:Length", 6);
        _otpExpiryMinutes = _configuration.GetValue<int>("Otp:ExpiryMinutes", 5);
        _maxAttempts = _configuration.GetValue<int>("Otp:MaxAttempts", 3);
        _maxOtpPerHour = _configuration.GetValue<int>("Otp:MaxPerHour", 5);
    }

    /// <inheritdoc/>
    public async Task<Result<string>> GenerateOtpAsync(
        string mobileNumber, 
        OtpPurpose purpose, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check rate limiting - max OTPs per hour
            var otpCount = await _otpCodeRepository.GetActiveOtpCountAsync(
                mobileNumber, purpose, TimeSpan.FromHours(1), cancellationToken);
            
            if (otpCount >= _maxOtpPerHour)
            {
                _logger.LogWarning("Rate limit exceeded for {MobileNumber} with purpose {Purpose}. Count: {Count}", 
                    mobileNumber, purpose, otpCount);
                return Result<string>.Failure("Too many OTP requests. Please try again later.");
            }
            
            // Check if there's an existing valid OTP
            var existingOtp = await _otpCodeRepository.GetLatestOtpAsync(mobileNumber, purpose, cancellationToken);
            if (existingOtp != null && existingOtp.IsValid)
            {
                _logger.LogInformation("Returning existing valid OTP for {MobileNumber} with purpose {Purpose}", 
                    mobileNumber, purpose);
                return Result<string>.Success(existingOtp.Code);
            }

            // Expire old OTPs for this mobile number and purpose
            await _otpCodeRepository.ExpireOldOtpsAsync(mobileNumber, purpose, cancellationToken);

            // Generate new OTP code
            var fixedOtp = _configuration.GetValue<string>("Otp:FixedOtp");
            var code = !string.IsNullOrEmpty(fixedOtp) ? fixedOtp : GenerateRandomCode(_otpLength);
            var expiresAt = DateTime.UtcNow.AddMinutes(_otpExpiryMinutes);
            
            // Get client info
            var ipAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();
            
            var otpCode = new Domain.Entities.OtpCode(
                mobileNumber, 
                code, 
                purpose, 
                expiresAt,
                ipAddress,
                userAgent);

            // Store in database
            await _otpCodeRepository.AddAsync(otpCode, cancellationToken);
            
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
            // First try to find OTP with the exact purpose
            var storedOtp = await _otpCodeRepository.GetValidOtpAsync(mobileNumber, otpCode, purpose, cancellationToken);
            
            // If not found and purpose is Login, try to find Registration OTP
            if (storedOtp == null && purpose == OtpPurpose.Login)
            {
                storedOtp = await _otpCodeRepository.GetValidOtpAsync(mobileNumber, otpCode, OtpPurpose.Registration, cancellationToken);
            }
            
            if (storedOtp == null)
            {
                _logger.LogWarning("OTP verification failed: No valid OTP found for {MobileNumber} with code {Code} and purpose {Purpose}", 
                    mobileNumber, otpCode, purpose);
                return Result<bool>.Success(false);
            }

            // Increment attempt count
            storedOtp.IncrementAttemptCount();
            
            // Check if too many attempts
            if (storedOtp.AttemptCount > _maxAttempts)
            {
                storedOtp.MarkAsUsed();
                await _otpCodeRepository.UpdateAsync(storedOtp, cancellationToken);
                
                _logger.LogWarning("OTP verification failed: Too many attempts for {MobileNumber} with purpose {Purpose}", 
                    mobileNumber, purpose);
                return Result<bool>.Success(false);
            }

            // Mark as used for certain purposes
            if (purpose == OtpPurpose.PasswordReset || purpose == OtpPurpose.PhoneVerification)
            {
                storedOtp.MarkAsUsed();
            }
            
            await _otpCodeRepository.UpdateAsync(storedOtp, cancellationToken);
            
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
            await _otpCodeRepository.ExpireOldOtpsAsync(mobileNumber, purpose, cancellationToken);
            
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

    /// <summary>
    /// Gets the client IP address from HTTP context
    /// </summary>
    private string? GetClientIpAddress()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                return ipAddress.Split(',')[0].Trim();
            }

            ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                return ipAddress;
            }

            return context.Connection.RemoteIpAddress?.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the user agent from HTTP context
    /// </summary>
    private string? GetUserAgent()
    {
        try
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.Request.Headers["User-Agent"].FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}