using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using Serilog;
using CoreAxis.Modules.ApiManager.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace CoreAxis.Modules.ApiManager.Application.Logging;

public class SensitiveDataMaskingEnricher : ILogEventEnricher
{
    private readonly ILoggingMaskingService _maskingService;
    
    public SensitiveDataMaskingEnricher(ILoggingMaskingService maskingService)
    {
        _maskingService = maskingService;
    }
    
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Mask the message template if it contains sensitive data
        if (logEvent.MessageTemplate?.Text != null)
        {
            var maskedMessage = _maskingService.MaskSensitiveData(logEvent.MessageTemplate.Text);
            if (maskedMessage != logEvent.MessageTemplate.Text)
            {
                var maskedProperty = propertyFactory.CreateProperty("OriginalMessage", logEvent.MessageTemplate.Text);
                logEvent.AddPropertyIfAbsent(maskedProperty);
            }
        }
        
        // Mask sensitive properties in the log event
        var propertiesToMask = new List<string>();
        
        foreach (var property in logEvent.Properties)
        {
            if (IsSensitiveProperty(property.Key) || ContainsSensitiveData(property.Value))
            {
                propertiesToMask.Add(property.Key);
            }
        }
        
        foreach (var propertyName in propertiesToMask)
        {
            if (logEvent.Properties.TryGetValue(propertyName, out var originalValue))
            {
                var maskedValue = MaskLogEventPropertyValue(originalValue);
                var maskedProperty = propertyFactory.CreateProperty(propertyName, maskedValue);
                logEvent.AddOrUpdateProperty(maskedProperty);
            }
        }
        
        // Add a marker to indicate this log has been processed for sensitive data
        var processedProperty = propertyFactory.CreateProperty("SensitiveDataProcessed", true);
        logEvent.AddPropertyIfAbsent(processedProperty);
    }
    
    private bool IsSensitiveProperty(string propertyName)
    {
        var sensitiveNames = new[] 
        {
            "password", "secret", "key", "token", "credential", "auth",
            "configjson", "config", "apikey", "accesstoken", "bearertoken",
            "clientsecret", "privatekey", "refreshtoken"
        };
        
        return sensitiveNames.Any(name => 
            propertyName.Contains(name, StringComparison.OrdinalIgnoreCase));
    }
    
    private bool ContainsSensitiveData(LogEventPropertyValue propertyValue)
    {
        if (propertyValue is ScalarValue scalarValue && scalarValue.Value is string stringValue)
        {
            // Check if the string looks like JSON and might contain sensitive data
            if (IsJsonLike(stringValue))
            {
                return ContainsSensitiveJsonData(stringValue);
            }
            
            // Check for common sensitive data patterns
            return ContainsSensitivePatterns(stringValue);
        }
        
        return false;
    }
    
    private bool IsJsonLike(string value)
    {
        return value.TrimStart().StartsWith("{") && value.TrimEnd().EndsWith("}");
    }
    
    private bool ContainsSensitiveJsonData(string jsonString)
    {
        try
        {
            using var document = JsonDocument.Parse(jsonString);
            return ContainsSensitiveJsonElement(document.RootElement);
        }
        catch (JsonException)
        {
            return false;
        }
    }
    
    private bool ContainsSensitiveJsonElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (IsSensitiveProperty(property.Name))
                    return true;
                    
                if (ContainsSensitiveJsonElement(property.Value))
                    return true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (ContainsSensitiveJsonElement(item))
                    return true;
            }
        }
        
        return false;
    }
    
    private bool ContainsSensitivePatterns(string value)
    {
        var sensitivePatterns = new[]
        {
            @"(?i)(password|pwd|secret|key|token|credential|auth)\s*[:=]",
            @"(?i)(api[_-]?key|access[_-]?token|bearer[_-]?token)\s*[:=]",
            @"(?i)(client[_-]?secret|private[_-]?key)\s*[:=]"
        };
        
        return sensitivePatterns.Any(pattern => 
            System.Text.RegularExpressions.Regex.IsMatch(value, pattern));
    }
    
    private object MaskLogEventPropertyValue(LogEventPropertyValue propertyValue)
    {
        if (propertyValue is ScalarValue scalarValue && scalarValue.Value is string stringValue)
        {
            if (IsJsonLike(stringValue))
            {
                return _maskingService.MaskConfigJson(stringValue);
            }
            
            return _maskingService.MaskSensitiveData(stringValue);
        }
        
        return "***MASKED***";
    }
}

// Extension method to easily add the enricher
public static class SensitiveDataMaskingEnricherExtensions
{
    public static LoggerConfiguration WithSensitiveDataMasking(
        this LoggerEnrichmentConfiguration enrichmentConfiguration,
        IServiceProvider serviceProvider)
    {
        var maskingService = serviceProvider.GetRequiredService<ILoggingMaskingService>();
        return enrichmentConfiguration.With(new SensitiveDataMaskingEnricher(maskingService));
    }
}