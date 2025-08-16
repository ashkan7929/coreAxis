using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.ApiManager.Application.Services;

public interface ILoggingMaskingService
{
    string MaskSensitiveData(string input);
    string MaskConfigJson(string configJson);
    T MaskSensitiveProperties<T>(T obj) where T : class;
}

public class LoggingMaskingService : ILoggingMaskingService
{
    private readonly ILogger<LoggingMaskingService> _logger;
    
    // Patterns for sensitive data
    private static readonly Regex[] SensitivePatterns = 
    {
        new Regex(@"(?i)(password|pwd|secret|key|token|credential|auth)\s*[:=]\s*[""']?([^\s,}""']+)", RegexOptions.Compiled),
        new Regex(@"(?i)(api[_-]?key|access[_-]?token|bearer[_-]?token)\s*[:=]\s*[""']?([^\s,}""']+)", RegexOptions.Compiled),
        new Regex(@"(?i)(client[_-]?secret|private[_-]?key)\s*[:=]\s*[""']?([^\s,}""']+)", RegexOptions.Compiled)
    };
    
    // JSON property names that should be masked
    private static readonly string[] SensitiveJsonProperties = 
    {
        "password", "secret", "key", "token", "credential", "auth",
        "apiKey", "api_key", "accessToken", "access_token", 
        "bearerToken", "bearer_token", "clientSecret", "client_secret",
        "privateKey", "private_key", "refreshToken", "refresh_token"
    };
    
    private const string MaskValue = "***MASKED***";
    
    public LoggingMaskingService(ILogger<LoggingMaskingService> logger)
    {
        _logger = logger;
    }
    
    public string MaskSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        var result = input;
        
        foreach (var pattern in SensitivePatterns)
        {
            result = pattern.Replace(result, match => 
            {
                var key = match.Groups[1].Value;
                return $"{key}: {MaskValue}";
            });
        }
        
        return result;
    }
    
    public string MaskConfigJson(string configJson)
    {
        if (string.IsNullOrEmpty(configJson))
            return configJson;
            
        try
        {
            using var document = JsonDocument.Parse(configJson);
            var maskedJson = MaskJsonElement(document.RootElement);
            return JsonSerializer.Serialize(maskedJson, new JsonSerializerOptions { WriteIndented = false });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON for masking, applying text-based masking");
            return MaskSensitiveData(configJson);
        }
    }
    
    public T MaskSensitiveProperties<T>(T obj) where T : class
    {
        if (obj == null)
            return obj;
            
        var json = JsonSerializer.Serialize(obj);
        var maskedJson = MaskConfigJson(json);
        
        try
        {
            return JsonSerializer.Deserialize<T>(maskedJson) ?? obj;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize masked object, returning original");
            return obj;
        }
    }
    
    private object MaskJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    var value = IsSensitiveProperty(property.Name) 
                        ? MaskValue 
                        : MaskJsonElement(property.Value);
                    dict[property.Name] = value;
                }
                return dict;
                
            case JsonValueKind.Array:
                return element.EnumerateArray().Select(MaskJsonElement).ToArray();
                
            case JsonValueKind.String:
                return element.GetString() ?? string.Empty;
                
            case JsonValueKind.Number:
                return element.TryGetInt64(out var longValue) ? longValue : element.GetDouble();
                
            case JsonValueKind.True:
                return true;
                
            case JsonValueKind.False:
                return false;
                
            case JsonValueKind.Null:
                return null;
                
            default:
                return element.ToString();
        }
    }
    
    private static bool IsSensitiveProperty(string propertyName)
    {
        return SensitiveJsonProperties.Any(sensitive => 
            string.Equals(propertyName, sensitive, StringComparison.OrdinalIgnoreCase));
    }
}