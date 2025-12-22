using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using CoreAxis.SharedKernel.Outbox;

using CoreAxis.SharedKernel.Ports;

namespace CoreAxis.Modules.ApiManager.Application.Security;

public class OAuth2AuthHandler : IAuthSchemeHandler
{
    private readonly ILogger<OAuth2AuthHandler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IDistributedCache? _distributedCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISecretResolver _secretResolver;

    public OAuth2AuthHandler(
        ILogger<OAuth2AuthHandler> logger, 
        IHttpClientFactory httpClientFactory, 
        IMemoryCache cache, 
        IHttpContextAccessor httpContextAccessor, 
        ISecretResolver secretResolver,
        IDistributedCache? distributedCache = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _httpContextAccessor = httpContextAccessor;
        _secretResolver = secretResolver;
        _distributedCache = distributedCache;
    }

    public SecurityType SupportedType => SecurityType.OAuth2;

    public async Task ApplyAsync(HttpRequestMessage request, SecurityProfile profile, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profile.ConfigJson)) return;

            var cfg = JsonSerializer.Deserialize<Dictionary<string, object>>(profile.ConfigJson);
            if (cfg == null) return;

            // Minimal support: use pre-provided token (APM-9 will add token caching and client credentials flow)
            if (cfg.TryGetValue("token", out var tokenObj))
            {
                var token = tokenObj?.ToString();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    return; // done
                }
            }

            // Client credentials flow with token cache (APM-9)
            var hasClientCreds = (cfg.ContainsKey("tokenEndpoint") || cfg.ContainsKey("tokenUrl"))
                                 && cfg.ContainsKey("clientId")
                                 && (cfg.ContainsKey("clientSecret") || cfg.ContainsKey("clientSecretRef"));
            if (hasClientCreds)
            {
                var tenantId = _httpContextAccessor.HttpContext?.Request?.Headers?["X-Tenant-Id"].ToString();
                var tokenEndpoint = cfg.TryGetValue("tokenEndpoint", out var te) && te is not null
                    ? te.ToString()!
                    : cfg["tokenUrl"].ToString()!;
                var clientId = cfg["clientId"].ToString()!;
                var scope = cfg.TryGetValue("scope", out var scopeObj) ? scopeObj?.ToString() ?? string.Empty : string.Empty;
                // Note: Secrets should be referenced via SecretRef; for demo, allow plain secret if present
                var clientSecret = cfg.TryGetValue("clientSecret", out var secretObj) ? secretObj?.ToString() : null;
                if (clientSecret == null && cfg.TryGetValue("clientSecretRef", out var secretRefObj))
                {
                    // Resolve secret ref
                    var secretRefKey = secretRefObj?.ToString();
                    if (!string.IsNullOrEmpty(secretRefKey))
                    {
                        // Try resolving via Secret Store (supports {{secret:KEY}})
                        var resolved = await _secretResolver.ResolveAsync(secretRefKey, cancellationToken);
                        if (!string.IsNullOrEmpty(resolved) && resolved != secretRefKey)
                        {
                            clientSecret = resolved;
                        }
                        else
                        {
                            // Fallback: Resolve from Environment/Configuration (legacy support for plain keys)
                            clientSecret = Environment.GetEnvironmentVariable(secretRefKey) ?? null;
                            if (string.IsNullOrEmpty(clientSecret))
                            {
                                clientSecret = _httpContextAccessor.HttpContext?.RequestServices?.GetService<IConfiguration>()?
                                    .GetValue<string>(secretRefKey);
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(clientSecret))
                {
                    _logger.LogWarning("OAuth2 client secret not resolved for ClientId {ClientId}. Ensure SecretRef is configured.", clientId);
                    return;
                }

                var preemptiveSeconds = cfg.TryGetValue("preemptiveRefreshSeconds", out var prsObj) && int.TryParse(prsObj?.ToString(), out var prs) ? Math.Max(30, prs) : 60;
                var cacheKey = $"apim:oauth2:{tenantId ?? "global"}:{clientId}:{scope}:{tokenEndpoint}";
                // Try distributed cache first (if available)
                TokenCacheEntry? entry = null;
                if (_distributedCache is not null)
                {
                    var cachedBlob = await _distributedCache.GetStringAsync(cacheKey, cancellationToken);
                    if (!string.IsNullOrEmpty(cachedBlob))
                    {
                        try
                        {
                            entry = JsonSerializer.Deserialize<TokenCacheEntry>(cachedBlob);
                        }
                        catch { /* ignore parse errors */ }
                    }
                }
                // Fallback to memory cache
                entry ??= _cache.TryGetValue<TokenCacheEntry>(cacheKey, out var memEntry) ? memEntry : null;
                if (entry is not null && !string.IsNullOrEmpty(entry.Token))
                {
                    var now = DateTime.UtcNow;
                    if (entry.ExpiresAtUtc - now > TimeSpan.FromSeconds(preemptiveSeconds))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", entry.Token);
                        return;
                    }
                }

                var httpClient = _httpClientFactory.CreateClient();
                using var form = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string,string>("grant_type", "client_credentials"),
                    new KeyValuePair<string,string>("client_id", clientId),
                    new KeyValuePair<string,string>("client_secret", clientSecret!),
                    new KeyValuePair<string,string>("scope", scope ?? string.Empty)
                });

                using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint) { Content = form };
                var tokenResponse = await httpClient.SendAsync(tokenRequest, cancellationToken);
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("OAuth2 token fetch failed: {StatusCode}", (int)tokenResponse.StatusCode);
                    return;
                }

                var json = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);
                var accessToken = doc.RootElement.TryGetProperty("access_token", out var tokenEl) ? tokenEl.GetString() : null;
                var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var expEl) ? expEl.GetInt32() : 3600;
                if (!string.IsNullOrEmpty(accessToken))
                {
                    var now = DateTime.UtcNow;
                    var expiresAtUtc = now.AddSeconds(expiresIn);
                    var ttl = TimeSpan.FromSeconds(Math.Max(60, expiresIn));
                    var cacheEntry = new TokenCacheEntry { Token = accessToken!, ExpiresAtUtc = expiresAtUtc };
                    // Save to distributed cache if available
                    if (_distributedCache is not null)
                    {
                        var blob = JsonSerializer.Serialize(cacheEntry);
                        var opts = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };
                        await _distributedCache.SetStringAsync(cacheKey, blob, opts, cancellationToken);
                    }
                    // Always save to memory cache as fast local fallback
                    _cache.Set(cacheKey, cacheEntry, ttl);
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    // Optional Outbox event emission (APM-13)
                    try
                    {
                        var emitOutbox = !(cfg.TryGetValue("emitOutbox", out var eo) && eo is not null && bool.TryParse(eo.ToString(), out var eoBool) && eoBool == false);
                        if (emitOutbox)
                        {
                            var outbox = _httpContextAccessor.HttpContext?.RequestServices?.GetService<IOutboxService>();
                            if (outbox is not null)
                            {
                                var corrHeader = _httpContextAccessor.HttpContext?.Request?.Headers?["X-Correlation-Id"].ToString();
                                var correlationId = Guid.TryParse(corrHeader, out var cid) ? cid : Guid.NewGuid();
                                var content = JsonSerializer.Serialize(new
                                {
                                    tenantId = tenantId ?? "global",
                                    clientId,
                                    scope,
                                    tokenEndpoint,
                                    expiresAtUtc
                                });
                                var msg = new OutboxMessage(
                                    type: "CoreAxis.ApiManager.OAuth.TokenRefreshed",
                                    content: content,
                                    correlationId: correlationId,
                                    causationId: null,
                                    tenantId: tenantId ?? "global"
                                );
                                await outbox.AddMessageAsync(msg, cancellationToken);
                                _logger.LogInformation("Outbox emitted: OAuth token refreshed for ClientId {ClientId}", clientId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to emit Outbox event for OAuth2 token refresh");
                    }
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth2 handler failed for SecurityProfile {ProfileId}", profile.Id);
        }

        return;
    }

    private sealed class TokenCacheEntry
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }
}