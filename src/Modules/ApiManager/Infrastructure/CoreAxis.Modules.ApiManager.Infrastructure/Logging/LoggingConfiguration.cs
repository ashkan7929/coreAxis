using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Infrastructure.Logging;

public static class LoggingConfiguration
{
    public static IServiceCollection AddApiManagerLogging(this IServiceCollection services)
    {
        // Add logging extensions for ApiManager
        services.AddLogging();
        
        return services;
    }
}

public static class LoggerExtensions
{
    public static void LogApiManagerStartup(this ILogger logger)
    {
        logger.LogInformation("ApiManager module logging initialized");
        logger.LogInformation("Sensitive data masking is enabled");
    }
    
    public static void LogSensitiveOperation(this ILogger logger, string operation, object? data = null)
    {
        if (data != null)
        {
            // Mask sensitive data before logging
            var maskedData = MaskSensitiveData(data);
            logger.LogInformation("Performing sensitive operation: {Operation} with data: {Data}", operation, maskedData);
        }
        else
        {
            logger.LogInformation("Performing sensitive operation: {Operation}", operation);
        }
    }
    
    public static void LogSecurityProfileOperation(this ILogger logger, string operation, string profileId, string? additionalInfo = null)
    {
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            // Mask additional info if it might contain sensitive data
            var maskedInfo = MaskSensitiveString(additionalInfo);
            logger.LogInformation("SecurityProfile operation: {Operation} for ProfileId: {ProfileId}, Info: {AdditionalInfo}", 
                operation, profileId, maskedInfo);
        }
        else
        {
            logger.LogInformation("SecurityProfile operation: {Operation} for ProfileId: {ProfileId}", 
                operation, profileId);
        }
    }
    
    public static void LogConfigurationAccess(this ILogger logger, string operation, string profileId)
    {
        logger.LogInformation("Configuration access: {Operation} for SecurityProfile {ProfileId} - Config details masked for security", 
            operation, profileId);
    }
    
    private static object MaskSensitiveData(object data)
    {
        // Simple masking - in production, use the LoggingMaskingService
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        return MaskSensitiveString(json);
    }
    
    private static string MaskSensitiveString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        // Simple regex-based masking for common sensitive patterns
        var patterns = new[]
        {
            (@"(?i)(password|pwd|secret|key|token|credential|auth)\s*[:=]\s*[""']?([^\s,}""']+)", "$1: ***MASKED***"),
            (@"(?i)(api[_-]?key|access[_-]?token|bearer[_-]?token)\s*[:=]\s*[""']?([^\s,}""']+)", "$1: ***MASKED***"),
            (@"(?i)(client[_-]?secret|private[_-]?key)\s*[:=]\s*[""']?([^\s,}""']+)", "$1: ***MASKED***")
        };
        
        var result = input;
        foreach (var (pattern, replacement) in patterns)
        {
            result = System.Text.RegularExpressions.Regex.Replace(result, pattern, replacement);
        }
        
        return result;
    }
}