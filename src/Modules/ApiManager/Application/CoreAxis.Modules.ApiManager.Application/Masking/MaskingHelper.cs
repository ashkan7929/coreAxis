using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using CoreAxis.Modules.ApiManager.Application.Services;
using CoreAxis.Modules.ApiManager.Domain;

namespace CoreAxis.Modules.ApiManager.Application.Masking;

public sealed class MaskingRules
{
    public List<string> HeaderKeysToMask { get; set; } = new();
    public List<string> BodyFieldsToMask { get; set; } = new();
    // New: Regex support for header keys (e.g., x-.*-token)
    public List<string> HeaderKeyRegexes { get; set; } = new();
    // New: JSONPath-like expressions for body masking (e.g., $.auth.token, order.items[0].card.number)
    public List<string> BodyJsonPaths { get; set; } = new();
}

public static class MaskingHelper
{
    private const string MaskValue = "***MASKED***";

    private static readonly string[] DefaultSensitiveHeaderKeys = new[]
    {
        "authorization", "x-api-key", "api-key", "x-auth-token", "x-signature",
        "signature", "cookie", "set-cookie", "x-amz-signature", "x-access-token",
        "x-client-secret", "client-secret", "x-private-key", "private-key"
    };

    public static string MaskHeaderValue(string key, string value, ILoggingMaskingService masking, MaskingRules? rules = null)
    {
        var k = key.ToLowerInvariant();
        var regexMatched = rules?.HeaderKeyRegexes?.Any() == true &&
            rules!.HeaderKeyRegexes!.Any(rx =>
                {
                    try { return Regex.IsMatch(key, rx, RegexOptions.IgnoreCase); }
                    catch { return false; }
                });

        if (regexMatched ||
            (rules?.HeaderKeysToMask?.Any() == true && rules!.HeaderKeysToMask!.Contains(k)) ||
            DefaultSensitiveHeaderKeys.Contains(k))
        {
            return MaskValue;
        }
        // Fallback to generic masking patterns
        return masking.MaskSensitiveData(value);
    }

    public static IEnumerable<KeyValuePair<string, string>> MaskHeaders(
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers,
        ILoggingMaskingService masking,
        MaskingRules? rules = null)
    {
        foreach (var header in headers)
        {
            var joined = string.Join(", ", header.Value);
            yield return new KeyValuePair<string, string>(header.Key, MaskHeaderValue(header.Key, joined, masking, rules));
        }
    }

    public static string MaskBody(string body, ILoggingMaskingService masking, MaskingRules? rules = null)
    {
        if (string.IsNullOrEmpty(body)) return body;

        var masked = masking.MaskSensitiveData(body);

        if (rules?.BodyFieldsToMask?.Any() == true)
        {
            foreach (var field in rules.BodyFieldsToMask)
            {
                // Simple JSON-like key masking: "field": "value" or "field": value
                var pattern = $"\"{Regex.Escape(field)}\"\\s*:\\s*\"?([^\",}}]+)\"?";
                masked = Regex.Replace(masked, pattern, $"\"{field}\" : \"{MaskValue}\"", RegexOptions.IgnoreCase);
            }
        }

        // JSONPath-like masking for precise body value paths
        if (rules?.BodyJsonPaths?.Any() == true)
        {
            try
            {
                var node = JsonNode.Parse(masked);
                if (node != null)
                {
                    foreach (var path in rules.BodyJsonPaths)
                    {
                        MaskJsonPath(node, path);
                    }
                    masked = node.ToJsonString();
                }
            }
            catch
            {
                // If JSON parsing fails, keep the text-masked fallback
            }
        }

        return masked;
    }

    private static void MaskJsonPath(JsonNode? root, string path)
    {
        if (root == null || string.IsNullOrWhiteSpace(path)) return;
        // Normalize: trim whitespace and optional leading '$.'
        var p = path.Trim();
        if (p.StartsWith("$.")) p = p.Substring(2);
        var segments = p.Split('.', StringSplitOptions.RemoveEmptyEntries);

        JsonNode? current = root;
        JsonNode? parent = null;
        string? lastKey = null;
        int? lastIndex = null;

        foreach (var seg in segments)
        {
            parent = current;
            lastKey = null;
            lastIndex = null;

            var name = seg;
            int? index = null;

            // Handle array index: e.g., items[0]
            var bracketStart = seg.IndexOf('[');
            if (bracketStart >= 0 && seg.EndsWith("]"))
            {
                name = seg.Substring(0, bracketStart);
                var idxStr = seg.Substring(bracketStart + 1, seg.Length - bracketStart - 2);
                if (int.TryParse(idxStr, out var idx)) index = idx;
            }

            if (parent is JsonObject obj)
            {
                current = obj[name];
                lastKey = name;
            }
            else if (parent is JsonArray arr)
            {
                if (index.HasValue && index.Value >= 0 && index.Value < arr.Count)
                {
                    current = arr[index.Value];
                    lastIndex = index.Value;
                }
                else
                {
                    current = null;
                }
            }
            else
            {
                current = null;
            }

            // If array index was provided after an object property
            if (index.HasValue && parent is JsonObject po)
            {
                var child = po[name] as JsonArray;
                if (child != null && index.Value >= 0 && index.Value < child.Count)
                {
                    parent = child;
                    current = child[index.Value];
                    lastKey = null;
                    lastIndex = index.Value;
                }
            }

            if (current == null)
            {
                // Path not found; stop
                return;
            }
        }

        // Set masked value at the target
        if (parent is JsonObject pobj && lastKey != null)
        {
            pobj[lastKey] = JsonValue.Create(MaskValue);
        }
        else if (parent is JsonArray parr && lastIndex.HasValue)
        {
            parr[lastIndex.Value] = JsonValue.Create(MaskValue);
        }
    }

    public static MaskingRules? ExtractRules(WebServiceMethod method)
    {
        // Placeholder for future per-endpoint mask rule fields (e.g., HeaderMaskRuleJson, BodyMaskJson)
        // Currently returns null and relies on default patterns.
        return null;
    }
}