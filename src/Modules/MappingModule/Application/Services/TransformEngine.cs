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
                SetValueByPath(output, rule.Target, value);
            }
        }
        
        return JsonSerializer.Serialize(output);
    }

    private void SetValueByPath(Dictionary<string, object> root, string path, object value)
    {
        // Normalize path: replace [ with . and remove ]
        // e.g. "orders[0].name" -> "orders.0.name"
        var normalized = path.Replace("[", ".").Replace("]", "");
        var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
        
        object current = root;
        
        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var isLast = i == parts.Length - 1;
            
            if (current is Dictionary<string, object> dict)
            {
                // Handle quoted keys in path (e.g. ['key.with.dot']) - naive cleanup
                part = part.Trim('\'', '"');

                if (isLast)
                {
                    dict[part] = value;
                }
                else
                {
                    // Check next part to decide structure
                    var nextPart = parts[i+1];
                    var isNextArray = int.TryParse(nextPart, out _);
                    
                    if (!dict.TryGetValue(part, out var nextVal) || nextVal == null)
                    {
                        nextVal = isNextArray ? new List<object>() : new Dictionary<string, object>();
                        dict[part] = nextVal;
                    }
                    else if (isNextArray && !(nextVal is List<object>))
                    {
                         // Conflict: expected list but found something else. Overwrite?
                         // For now, keep existing unless it's incompatible type?
                         // If we need list but have dict, we have a problem.
                         // Let's assume we overwrite if type mismatch
                         nextVal = new List<object>();
                         dict[part] = nextVal;
                    }
                    else if (!isNextArray && !(nextVal is Dictionary<string, object>))
                    {
                        nextVal = new Dictionary<string, object>();
                        dict[part] = nextVal;
                    }
                    
                    current = nextVal;
                }
            }
            else if (current is List<object> list)
            {
                if (!int.TryParse(part, out var index))
                {
                    // Trying to access property on list? Invalid path for this structure.
                    return;
                }
                
                // Ensure capacity
                while (list.Count <= index) list.Add(null);
                
                if (isLast)
                {
                    list[index] = value;
                }
                else
                {
                    var nextPart = parts[i+1];
                    var isNextArray = int.TryParse(nextPart, out _);
                    
                    var nextVal = list[index];
                    if (nextVal == null)
                    {
                        nextVal = isNextArray ? new List<object>() : new Dictionary<string, object>();
                        list[index] = nextVal;
                    }
                    else if (isNextArray && !(nextVal is List<object>))
                    {
                        nextVal = new List<object>();
                        list[index] = nextVal;
                    }
                    else if (!isNextArray && !(nextVal is Dictionary<string, object>))
                    {
                        nextVal = new Dictionary<string, object>();
                        list[index] = nextVal;
                    }
                    
                    current = nextVal;
                }
            }
        }
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

            case "regex_replace":
                // regex_replace(input, pattern, replacement)
                if (values.Count >= 3 && values[0] is string inputStr && values[1] is string pattern && values[2] is string replacement)
                {
                    try { return Task.FromResult<object?>(Regex.Replace(inputStr, pattern, replacement)); }
                    catch { return Task.FromResult<object?>(inputStr); }
                }
                return Task.FromResult<object?>(null);

            case "if":
                // if(condition, trueVal, falseVal)
                if (values.Count >= 3 && values[0] is bool condition)
                    return Task.FromResult(condition ? values[1] : values[2]);
                return Task.FromResult<object?>(null);

            case "round":
                // round(value, decimals)
                if (values.Count >= 1 && decimal.TryParse(values[0]?.ToString(), out var valToRound))
                {
                    int decimals = 0;
                    if (values.Count >= 2 && int.TryParse(values[1]?.ToString(), out var d)) decimals = d;
                    return Task.FromResult<object?>(Math.Round(valToRound, decimals));
                }
                return Task.FromResult<object?>(null);

            case "lookup":
                // lookup(key, dictionaryJson)
                // Dictionary must be a JSON object string or JsonElement
                if (values.Count >= 2 && values[0] is string key)
                {
                    try 
                    {
                        var dict = values[1] is JsonElement je ? je : 
                                   values[1] is string js ? JsonDocument.Parse(json: js).RootElement : default;
                        
                        if (dict.ValueKind == JsonValueKind.Object && dict.TryGetProperty(key, out var val))
                        {
                            return Task.FromResult<object?>(val.ToString());
                        }
                    }
                    catch {}
                }
                return Task.FromResult<object?>(null);

            default:
                return Task.FromResult<object?>(null);
        }
    }

    private object? GetValueByPath(JsonElement element, string path)
    {
        // Normalize path: replace [ with . and remove ]
        // e.g. "orders[0].name" -> "orders.0.name"
        // "matrix[0][1]" -> "matrix.0.1"
        
        var normalized = path.Replace("[", ".").Replace("]", "");
        var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
        
        var current = element;
        
        foreach (var part in parts)
        {
            // Check if array index
            if (current.ValueKind == JsonValueKind.Array && int.TryParse(part, out var index))
            {
                if (index >= 0 && index < current.GetArrayLength())
                {
                    current = current[index];
                    continue;
                }
                return null; // Index out of bounds
            }
            
            // Assume property
            string propName = part.Trim('\'', '"'); // Handle ['prop']
            
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(propName, out var next))
            {
                current = next;
            }
            else
            {
                return null; // Property not found or not an object
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
