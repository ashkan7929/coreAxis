using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Orchestrates fetching external data via ApiManager with TTL caching for formula evaluations.
    /// </summary>
    public class DataOrchestrator : IDataOrchestrator
    {
        private readonly IApiProxy _apiProxy;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DataOrchestrator> _logger;
        private readonly ConcurrentDictionary<string, byte> _cacheKeys = new();

        private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

        public DataOrchestrator(
            IApiProxy apiProxy,
            IMemoryCache cache,
            ILogger<DataOrchestrator> logger)
        {
            _apiProxy = apiProxy ?? throw new ArgumentNullException(nameof(apiProxy));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Dictionary<string, object?>>> GetExternalDataAsync(
            Dictionary<string, object?> context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = new Dictionary<string, object?>();

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

                foreach (var (key, cfgObj) in sources)
                {
                    try
                    {
                        if (cfgObj is not Dictionary<string, object?> cfg)
                        {
                            _logger.LogWarning("Source '{SourceKey}' has invalid configuration type.", key);
                            continue;
                        }

                        // methodId can be Guid or string-Guid
                        Guid methodId = Guid.Empty;
                        if (cfg.TryGetValue("methodId", out var methodObj))
                        {
                            if (methodObj is Guid g) methodId = g;
                            else if (methodObj is string s && Guid.TryParse(s, out var parsed)) methodId = parsed;
                        }

                        var parameters = cfg.TryGetValue("parameters", out var pObj) && pObj is Dictionary<string, object?> pDict
                            ? pDict
                            : new Dictionary<string, object?>();

                        // Optional per-source TTL settings
                        // Supports keys: ttlSeconds, cacheTtlSeconds, ttlMinutes, slidingSeconds
                        int ttlSeconds = 0;
                        if (TryGetInt(cfg, "ttlSeconds", out var ttlSec)) ttlSeconds = ttlSec;
                        else if (TryGetInt(cfg, "cacheTtlSeconds", out var cacheTtl)) ttlSeconds = cacheTtl;
                        else if (TryGetInt(cfg, "ttlMinutes", out var ttlMin)) ttlSeconds = ttlMin * 60;

                        if (ttlSeconds > 0)
                        {
                            parameters["_ttlSeconds"] = ttlSeconds;
                        }

                        if (TryGetInt(cfg, "slidingSeconds", out var slidingSeconds) && slidingSeconds > 0)
                        {
                            parameters["_slidingSeconds"] = slidingSeconds;
                        }

                        var valueRes = await GetValueAsync(methodId, parameters, cancellationToken);
                        if (valueRes.IsFailure)
                        {
                            _logger.LogWarning("Failed to fetch external data for '{SourceKey}': {Error}", key, valueRes.Error);
                            continue;
                        }

                        result[key] = valueRes.Value;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing external source '{SourceKey}'.", key);
                    }
                }

                return Result.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetExternalDataAsync");
                return Result.Failure<Dictionary<string, object?>>(ex.Message);
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

                // Convert parameters to required signature for IApiProxy
                var plainParams = new Dictionary<string, object>();
                foreach (var kv in parameters)
                {
                    if (kv.Value is null) continue;
                    // Exclude internal orchestration hints
                    if (kv.Key.StartsWith("_")) continue;
                    plainParams[kv.Key] = kv.Value!;
                }

                var proxyResult = await _apiProxy.InvokeAsync(webServiceMethodId, plainParams, cancellationToken);
                if (!proxyResult.IsSuccess)
                {
                    var msg = proxyResult.ErrorMessage ?? "ApiProxy call failed";
                    _logger.LogWarning("ApiProxy failure for {MethodId}: {Message}", webServiceMethodId, msg);
                    return Result.Failure<object?>(msg);
                }

                object? parsed = proxyResult.ResponseBody;
                // Best-effort JSON parse to object graph if response is JSON
                if (proxyResult.ResponseBody is string sBody)
                {
                    try
                    {
                        parsed = JsonSerializer.Deserialize<object>(sBody);
                    }
                    catch
                    {
                        // keep raw string if not JSON
                        parsed = sBody;
                    }
                }

                // Resolve per-source TTL from parameters if provided
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
                default:
                    value = 0;
                    return false;
            }
        }
    }
}