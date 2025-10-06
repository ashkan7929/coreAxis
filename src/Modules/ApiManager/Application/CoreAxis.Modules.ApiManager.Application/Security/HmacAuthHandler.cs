using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.ApiManager.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CoreAxis.SharedKernel.Outbox;

namespace CoreAxis.Modules.ApiManager.Application.Security;

public class HmacAuthHandler : IAuthSchemeHandler
{
    private readonly ILogger<HmacAuthHandler> _logger;
    private readonly IHmacCanonicalSigner _signer;
    private readonly ITimestampProvider _clock;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HmacAuthHandler(ILogger<HmacAuthHandler> logger, IHmacCanonicalSigner signer, ITimestampProvider clock, IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _signer = signer;
        _clock = clock;
        _httpContextAccessor = httpContextAccessor;
    }

    public SecurityType SupportedType => SecurityType.HMAC;

    public Task ApplyAsync(HttpRequestMessage request, SecurityProfile profile, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(profile.ConfigJson)) return Task.CompletedTask;
            var cfg = JsonSerializer.Deserialize<Dictionary<string, object>>(profile.ConfigJson);
            if (cfg == null) return Task.CompletedTask;

            // Expected config: { headerName: "X-Signature", timestampHeader: "X-Timestamp", includeBodyHash: true, bodyHashAlgorithm: "SHA256", secret: "...", algorithm: "HMACSHA256" }
            var headerName = cfg.TryGetValue("headerName", out var h) ? h?.ToString() : "X-Signature";
            var timestampHeader = cfg.TryGetValue("timestampHeader", out var th) ? th?.ToString() : "X-Timestamp";
            var includeBodyHash = cfg.TryGetValue("includeBodyHash", out var ibh) && bool.TryParse(ibh?.ToString(), out var ibhBool) ? ibhBool : true;
            var bodyHashAlgorithm = cfg.TryGetValue("bodyHashAlgorithm", out var bha) ? bha?.ToString() : "SHA256";
            var secret = cfg.TryGetValue("secret", out var s) ? s?.ToString() : null;
            // Optional clock-skew tolerance header support (default ±300 seconds)
            var toleranceSeconds = cfg.TryGetValue("toleranceSeconds", out var tol) && int.TryParse(tol?.ToString(), out var tolInt) ? tolInt : 300;
            var toleranceHeader = cfg.TryGetValue("toleranceHeader", out var thdr) ? thdr?.ToString() : "X-Time-Tolerance";
            if (secret == null && cfg.TryGetValue("secretRef", out var secretRefObj))
            {
                var secretRefKey = secretRefObj?.ToString();
                if (!string.IsNullOrEmpty(secretRefKey))
                {
                    // Resolve from environment first, then configuration
                    secret = Environment.GetEnvironmentVariable(secretRefKey) ?? null;
                    if (string.IsNullOrEmpty(secret))
                    {
                        secret = _httpContextAccessor.HttpContext?.RequestServices?.GetService<IConfiguration>()?
                            .GetValue<string>(secretRefKey);
                    }
                }
            }
            var algo = cfg.TryGetValue("algorithm", out var a) ? a?.ToString() : "HMACSHA256";

            if (string.IsNullOrWhiteSpace(secret)) return Task.CompletedTask;

            var timestamp = _clock.UtcNow().ToUnixTimeSeconds().ToString();
            var path = request.RequestUri?.AbsolutePath ?? "/";
            var query = request.RequestUri?.Query?.TrimStart('?') ?? string.Empty;

            string? bodyHash = null;
            if (includeBodyHash && request.Content != null)
            {
                var content = request.Content.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(content))
                {
                    bodyHash = _signer.ComputeBodyHash(content, bodyHashAlgorithm!);
                }
            }

            var signature = _signer.ComputeSignature(
                request.Method.Method,
                path,
                query,
                timestamp,
                bodyHash,
                secret!,
                algo!);

            // Include timestamp header as well for server validation
            if (!string.IsNullOrWhiteSpace(timestampHeader))
            {
                request.Headers.TryAddWithoutValidation(timestampHeader!, timestamp);
            }
            // Provide tolerance hint header for receiver-side validation if configured
            if (!string.IsNullOrWhiteSpace(toleranceHeader) && toleranceSeconds > 0)
            {
                request.Headers.TryAddWithoutValidation(toleranceHeader!, toleranceSeconds.ToString());
            }
            if (!string.IsNullOrWhiteSpace(headerName))
            {
                request.Headers.TryAddWithoutValidation(headerName!, signature);
            }

            // Optional Outbox event emission (APM-13) without sensitive data
            try
            {
                var outbox = _httpContextAccessor.HttpContext?.RequestServices?.GetService<IOutboxService>();
                if (outbox is not null)
                {
                    var corrHeader = _httpContextAccessor.HttpContext?.Request?.Headers?["X-Correlation-Id"].ToString();
                    var correlationId = Guid.TryParse(corrHeader, out var cid) ? cid : Guid.NewGuid();
                    var tenantId = _httpContextAccessor.HttpContext?.Request?.Headers?["X-Tenant-Id"].ToString();
                    var content = JsonSerializer.Serialize(new
                    {
                        tenantId = tenantId ?? "global",
                        method = request.Method?.Method,
                        path,
                        query,
                        headerName,
                        timestampHeader,
                        includeBodyHash,
                        bodyHashAlgorithm,
                        algorithm = algo
                    });
                    var msg = new OutboxMessage(
                        type: "CoreAxis.ApiManager.HMAC.SignatureApplied",
                        content: content,
                        correlationId: correlationId,
                        causationId: null,
                        tenantId: tenantId ?? "global"
                    );
                    outbox.AddMessageAsync(msg, cancellationToken).GetAwaiter().GetResult();
                    _logger.LogInformation("Outbox emitted: HMAC signature applied on {Method} {Path}", request.Method?.Method, path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to emit Outbox event for HMAC signature");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HMAC handler failed for SecurityProfile {ProfileId}", profile.Id);
        }

        return Task.CompletedTask;
    }

    private static HMAC CreateHmac(string algorithm, byte[] key)
    {
        return algorithm.ToUpperInvariant() switch
        {
            "HMACSHA1" => new HMACSHA1(key),
            "HMACSHA256" => new HMACSHA256(key),
            "HMACSHA384" => new HMACSHA384(key),
            "HMACSHA512" => new HMACSHA512(key),
            _ => new HMACSHA256(key)
        };
    }
}