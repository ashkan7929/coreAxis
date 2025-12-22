using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Orchestrates fetching external data via ApiManager with TTL caching for formula evaluations.
    /// Supports dependency resolution between sources.
    /// </summary>
    public class DataOrchestrator : IDataOrchestrator
    {
        private readonly IApiProxy _apiProxy;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DataOrchestrator> _logger;
        private readonly ConcurrentDictionary<string, byte> _cacheKeys = new();

        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
        // Regex to match @{sourceName} or @{sourceName.prop.subProp}
        private static readonly Regex VariableRefRegex = new Regex(@"^@\{([^}]+)\}$", RegexOptions.Compiled);

        public DataOrchestrator(
            IApiProxy apiProxy,
            IMemoryCache cache,
            ILogger<DataOrchestrator> logger)
        {
            _apiProxy = apiProxy ?? throw new ArgumentNullException(nameof(apiProxy));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ExternalDataBatchResult>> GetExternalDataAsync(
            Dictionary<string, object?> context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = new ExternalDataBatchResult();

                if (!context.TryGetValue("externalDataSources", out var sourcesObj) || sourcesObj is null)
                {
                    _logger.LogDebug("No externalDataSources provided in context.");
                    return Result.Success(result);
                }

                if (sourcesObj is not Dictionary<string, object?> sources)
                {
                    _logger.LogWarning("externalDataSources must be a dictionary of sources.");
                    return Result.Success(result);
                }

                // 1. Build Dependency Graph
                var graph = new DependencyGraph();
                var sourceConfigs = new Dictionary<string, Dictionary<string, object?>>();

                foreach (var (key, cfgObj) in sources)
                {
                    if (cfgObj is not Dictionary<string, object?> cfg)
                    {
                        _logger.LogWarning("Source '{SourceKey}' has invalid configuration type.", key);
                        continue;
                    }
                    sourceConfigs[key] = cfg;

                    var parameters = cfg.TryGetValue("parameters", out var pObj) && pObj is Dictionary<string, object?> pDict
                        ? pDict
                        : new Dictionary<string, object?>();

                    foreach (var paramVal in parameters.Values)
                    {
                        if (paramVal is string s)
                        {
                            var match = VariableRefRegex.Match(s);
                            if (match.Success)
                            {
                                var refPath = match.Groups[1].Value;
                                var refSource = refPath.Split('.')[0]; // Assumes first part is source name
                                
                                // Only add dependency if it refers to another source in the list
                                if (sources.ContainsKey(refSource) && !refSource.Equals(key, StringComparison.OrdinalIgnoreCase))
                                {
                                    graph.AddDependency(key, refSource);
                                }
                            }
                        }
                    }
                }

                // 2. Get Execution Order
                IEnumerable<string> executionOrder;
                try
                {
                    var orderedDependent = graph.GetTopologicalOrder().ToList();
                    var allKeys = sourceConfigs.Keys.ToList();
                    // Topological order only contains items with dependencies (or dependents).
                    // We need to ensure we cover all items.
                    // Actually GetTopologicalOrder implementation traverses all nodes added to _dependencies.
                    // But if a node has no dependencies and no dependents, it might not be in the graph if we didn't add it.
                    // DependencyGraph.AddDependency adds both.
                    // If a source has no deps, we didn't call AddDependency.
                    // So we need to merge with independent items.
                    
                    var independent = allKeys.Except(orderedDependent, StringComparer.OrdinalIgnoreCase).ToList();
                    executionOrder = independent.Concat(orderedDependent);
                }
                catch (InvalidOperationException ex)
                {
                     _logger.LogError(ex, "Circular dependency detected in external sources.");
                     return Result.Failure<ExternalDataBatchResult>("Circular dependency in external data sources.");
                }

                // 3. Execute
                foreach (var key in executionOrder)
                {
                    if (!sourceConfigs.TryGetValue(key, out var cfg)) continue;

                    try
                    {
                        Guid methodId = Guid.Empty;
                        if (cfg.TryGetValue("methodId", out var methodObj))
                        {
                            if (methodObj is Guid g) methodId = g;
                            else if (methodObj is string s && Guid.TryParse(s, out var parsed)) methodId = parsed;
                        }

                        var parameters = cfg.TryGetValue("parameters", out var pObj) && pObj is Dictionary<string, object?> pDict
                            ? new Dictionary<string, object?>(pDict)
                            : new Dictionary<string, object?>();

                        // Resolve parameters
                        foreach (var pKey in parameters.Keys.ToList())
                        {
                            if (parameters[pKey] is string s)
                            {
                                var match = VariableRefRegex.Match(s);
                                if (match.Success)
                                {
                                    var refPath = match.Groups[1].Value;
                                    var resolvedValue = ResolvePath(refPath, result.Data, context);
                                    parameters[pKey] = resolvedValue;
                                }
                            }
                        }

                        int ttlSeconds = 0;
                        if (TryGetInt(cfg, "ttlSeconds", out var ttlSec)) ttlSeconds = ttlSec;
                        else if (TryGetInt(cfg, "cacheTtlSeconds", out var cacheTtl)) ttlSeconds = cacheTtl;
                        else if (TryGetInt(cfg, "ttlMinutes", out var ttlMin)) ttlSeconds = ttlMin * 60;

                        if (ttlSeconds > 0) parameters["_ttlSeconds"] = ttlSeconds;
                        if (TryGetInt(cfg, "slidingSeconds", out var slidingSeconds) && slidingSeconds > 0) parameters["_slidingSeconds"] = slidingSeconds;

                        var valueRes = await GetValueAsync(methodId, parameters, cancellationToken);
                        if (!valueRes.IsSuccess)
                        {
                             _logger.LogWarning("Failed to fetch external data for '{SourceKey}': {Error}", key, string.Join(", ", valueRes.Errors));
                             result.Trace[key] = $"Failed: {string.Join(", ", valueRes.Errors)}";
                             continue;
                        }

                        result.Data[key] = valueRes.Value;
                        result.Trace[key] = "Success";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing external source '{SourceKey}'.", key);
                        result.Trace[key] = $"Error: {ex.Message}";
                    }
                }

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExternalDataAsync");
                return Result.Failure<ExternalDataBatchResult>(ex.Message);
            }
        }

        public async Task<Result<object?>> GetValueAsync(
            Guid webServiceMethodId,
            Dictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (webServiceMethodId == Guid.Empty)
                {
                    return Result.Failure<object?>("webServiceMethodId is empty");
                }

                var cacheKey = BuildCacheKey(webServiceMethodId, parameters);

                if (_cache.TryGetValue(cacheKey, out object? cached))
                {
                    _logger.LogDebug("Cache hit for method {MethodId}", webServiceMethodId);
                    return Result.Success(cached);
                }

                var plainParams = new Dictionary<string, object>();
                foreach (var kv in parameters)
                {
                    if (kv.Value is null) continue;
                    if (kv.Key.StartsWith("_")) continue;
                    plainParams[kv.Key] = kv.Value!;
                }

                var proxyResult = await _apiProxy.InvokeAsync(webServiceMethodId, plainParams, null, null, cancellationToken);
                if (!proxyResult.IsSuccess)
                {
                    var msg = proxyResult.ErrorMessage ?? "ApiProxy call failed";
                    _logger.LogWarning("ApiProxy failure for {MethodId}: {Message}", webServiceMethodId, msg);
                    return Result.Failure<object?>(msg);
                }

                object? parsed = proxyResult.ResponseBody;
                if (proxyResult.ResponseBody is string sBody)
                {
                    try
                    {
                        parsed = JsonSerializer.Deserialize<object>(sBody);
                    }
                    catch
                    {
                        parsed = sBody;
                    }
                }

                var ttl = DefaultTtl;
                if (parameters.TryGetValue("_ttlSeconds", out var ttlObj) && TryConvertToInt(ttlObj, out var ttlSeconds) && ttlSeconds > 0)
                {
                    ttl = TimeSpan.FromSeconds(ttlSeconds);
                }

                TimeSpan? sliding = TimeSpan.FromMinutes(2);
                if (parameters.TryGetValue("_slidingSeconds", out var slidingObj) && TryConvertToInt(slidingObj, out var slidingSeconds) && slidingSeconds > 0)
                {
                    sliding = TimeSpan.FromSeconds(slidingSeconds);
                }

                var options = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl,
                    SlidingExpiration = sliding
                };
                _cache.Set(cacheKey, parsed, options);
                _cacheKeys[cacheKey] = 1;

                return Result.Success(parsed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching external value for method {MethodId}", webServiceMethodId);
                return Result.Failure<object?>(ex.Message);
            }
        }

        public Task ClearCacheAsync(string cacheKeyPrefix, CancellationToken cancellationToken = default)
        {
            foreach (var key in _cacheKeys.Keys)
            {
                if (key.StartsWith(cacheKeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    _cache.Remove(key);
                    _cacheKeys.TryRemove(key, out _);
                }
            }
            return Task.CompletedTask;
        }

        private string BuildCacheKey(Guid methodId, Dictionary<string, object?> parameters)
        {
            // We should only use "plain" parameters for cache key, excluding transient things if any?
            // But here all parameters matter.
            var paramJson = JsonSerializer.Serialize(parameters);
            var hash = paramJson.GetHashCode();
            return $"external_{methodId}_{hash}";
        }

        private static bool TryGetInt(Dictionary<string, object?> dict, string key, out int value)
        {
            value = 0;
            if (!dict.TryGetValue(key, out var obj) || obj is null) return false;
            return TryConvertToInt(obj, out value);
        }

        private static bool TryConvertToInt(object obj, out int value)
        {
            switch (obj)
            {
                case int i:
                    value = i;
                    return true;
                case long l:
                    value = (int)l;
                    return true;
                case string s when int.TryParse(s, out var parsed):
                    value = parsed;
                    return true;
                case JsonElement je when je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out var jeInt):
                    value = jeInt;
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }

        private object? ResolvePath(string path, Dictionary<string, object?> calculatedData, Dictionary<string, object?> context)
        {
            var parts = path.Split('.');
            if (parts.Length == 0) return null;

            var root = parts[0];
            object? current = null;

            if (calculatedData.TryGetValue(root, out var val))
            {
                current = val;
            }
            else if (context.TryGetValue(root, out var ctxVal))
            {
                current = ctxVal;
            }
            else
            {
                return null;
            }

            for (int i = 1; i < parts.Length; i++)
            {
                if (current is null) return null;

                if (current is JsonElement je)
                {
                    if (je.ValueKind == JsonValueKind.Object && je.TryGetProperty(parts[i], out var prop))
                    {
                        current = prop;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (current is Dictionary<string, object?> dict)
                {
                    if (dict.TryGetValue(parts[i], out var next)) current = next;
                    else return null;
                }
                else if (current is Dictionary<string, object> dict2)
                {
                    if (dict2.TryGetValue(parts[i], out var next)) current = next;
                    else return null;
                }
                else
                {
                    try
                    {
                        var prop = current.GetType().GetProperty(parts[i]);
                        if (prop != null) current = prop.GetValue(current);
                        else return null;
                    }
                    catch { return null; }
                }
            }

            // Unwrap JsonElement if it's a primitive
            if (current is JsonElement jeFinal)
            {
                switch (jeFinal.ValueKind)
                {
                    case JsonValueKind.String: return jeFinal.GetString();
                    case JsonValueKind.Number: return jeFinal.GetDouble(); // safe fallback
                    case JsonValueKind.True: return true;
                    case JsonValueKind.False: return false;
                    case JsonValueKind.Null: return null;
                    default: return jeFinal; // Return element for object/array
                }
            }

            return current;
        }
    }
}