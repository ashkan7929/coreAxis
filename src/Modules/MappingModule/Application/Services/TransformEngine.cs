using CoreAxis.Modules.MappingModule.Application.DTOs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CoreAxis.Modules.MappingModule.Application.Services;

public class TransformEngine : ITransformEngine
{
    public async Task<string> ExecuteAsync(string rulesJson, string contextJson, CancellationToken cancellationToken = default)
    {
        using var doc = JsonDocument.Parse(contextJson);
        var root = doc.RootElement;
        
        var output = new Dictionary<string, object>();
        
        List<MappingRule>? rules;
        try 
        {
            rules = JsonSerializer.Deserialize<List<MappingRule>>(rulesJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return "{}"; // Invalid rules
        }
        
        if (rules == null) return "{}";

        foreach (var rule in rules)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            var value = await EvaluateExpressionAsync(rule.Expression, root, cancellationToken);
            if (value != null)
            {
                output[rule.Target] = value;
            }
        }
        
        return JsonSerializer.Serialize(output);
    }

    public Task<object?> EvaluateExpressionAsync(string expression, JsonElement context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expression)) return Task.FromResult<object?>(null);
        expression = expression.Trim();

        // 1. Literal string: 'value'
        if (expression.StartsWith("'") && expression.EndsWith("'"))
        {
            return Task.FromResult<object?>(expression.Trim('\''));
        }
        
        // 2. JSON Path: $.path
        if (expression.StartsWith("$."))
        {
            return Task.FromResult(GetValueByPath(context, expression.Substring(2)));
        }
        
        // 3. Functions (Basic implementation)
        // concat('a', 'b')
        // toInt('123')
        
        if (expression.Contains("("))
        {
            // Simple parsing: Name(args)
            // This is naive and doesn't handle nested functions well without a proper parser
            var match = Regex.Match(expression, @"^(\w+)\((.*)\)$");
            if (match.Success)
            {
                var funcName = match.Groups[1].Value.ToLower();
                var argsStr = match.Groups[2].Value;
                
                // Split args by comma, respecting quotes? 
                // For MVP, simple split
                var args = argsStr.Split(',').Select(a => a.Trim()).ToArray();
                
                // Evaluate args recursively?
                // For MVP, assume args are literals or paths.
                
                return EvaluateFunction(funcName, args, context);
            }
        }

        return Task.FromResult<object?>(null);
    }

    private Task<object?> EvaluateFunction(string name, string[] args, JsonElement context)
    {
        // Resolve args values
        var values = new List<object?>();
        foreach(var arg in args)
        {
            // Synchronous evaluation for args (simplified)
            // We strip quotes if literal, or resolve path
            if (arg.StartsWith("'") && arg.EndsWith("'"))
            {
                values.Add(arg.Trim('\''));
            }
            else if (arg.StartsWith("$."))
            {
                values.Add(GetValueByPath(context, arg.Substring(2)));
            }
            else
            {
                // Number or boolean or null
                if (decimal.TryParse(arg, out var d)) values.Add(d);
                else if (bool.TryParse(arg, out var b)) values.Add(b);
                else values.Add(arg); // fallback
            }
        }

        switch (name)
        {
            case "concat":
                return Task.FromResult<object?>(string.Join("", values.Select(v => v?.ToString() ?? "")));
            
            case "coalesce":
                return Task.FromResult(values.FirstOrDefault(v => v != null));
                
            case "toint":
                if (values.Count > 0 && int.TryParse(values[0]?.ToString(), out var i))
                    return Task.FromResult<object?>(i);
                return Task.FromResult<object?>(null);
                
            case "todecimal":
                if (values.Count > 0 && decimal.TryParse(values[0]?.ToString(), out var dec))
                    return Task.FromResult<object?>(dec);
                return Task.FromResult<object?>(null);

            case "add":
                if (values.Count >= 2 && 
                    decimal.TryParse(values[0]?.ToString(), out var d1) && 
                    decimal.TryParse(values[1]?.ToString(), out var d2))
                    return Task.FromResult<object?>(d1 + d2);
                return Task.FromResult<object?>(null);

            case "sub":
                if (values.Count >= 2 && 
                    decimal.TryParse(values[0]?.ToString(), out var s1) && 
                    decimal.TryParse(values[1]?.ToString(), out var s2))
                    return Task.FromResult<object?>(s1 - s2);
                return Task.FromResult<object?>(null);

            case "todate":
                if (values.Count > 0 && DateTime.TryParse(values[0]?.ToString(), out var dt))
                    return Task.FromResult<object?>(dt);
                return Task.FromResult<object?>(null);

            case "formatdate":
                // formatdate(date, format)
                if (values.Count >= 2 && 
                    DateTime.TryParse(values[0]?.ToString(), out var fdt) && 
                    values[1] is string fmt)
                    return Task.FromResult<object?>(fdt.ToString(fmt));
                return Task.FromResult<object?>(null);

            default:
                return Task.FromResult<object?>(null);
        }
    }

    private object? GetValueByPath(JsonElement element, string path)
    {
        var parts = path.Split('.');
        var current = element;
        
        foreach (var part in parts)
        {
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var next))
            {
                current = next;
            }
            else
            {
                return null;
            }
        }
        
        switch (current.ValueKind)
        {
            case JsonValueKind.String: return current.GetString();
            case JsonValueKind.Number: return current.GetDecimal();
            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;
            default: return current.GetRawText();
        }
    }
}
