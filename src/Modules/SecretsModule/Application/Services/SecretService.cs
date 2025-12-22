using CoreAxis.Modules.SecretsModule.Application.Contracts;
using CoreAxis.SharedKernel.Ports;
using CoreAxis.Modules.SecretsModule.Domain.Entities;
using CoreAxis.Modules.SecretsModule.Infrastructure.Data;
using CoreAxis.SharedKernel.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.SecretsModule.Application.Services;

public class SecretService : ISecretResolver
{
    private readonly SecretsDbContext _context;
    private readonly IEncryptionService _encryptionService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SecretService> _logger;

    public SecretService(
        SecretsDbContext context,
        IEncryptionService encryptionService,
        ITenantProvider tenantProvider,
        ILogger<SecretService> logger)
    {
        _context = context;
        _encryptionService = encryptionService;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task SetSecretAsync(string key, string value, string? description = null, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.TenantId ?? "default";
        var encryptedValue = _encryptionService.Encrypt(value);

        var existingSecret = await _context.Secrets
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (existingSecret != null)
        {
            existingSecret.UpdateValue(encryptedValue);
            // Description update logic if needed, skipping for now to keep it simple or add a method in entity
        }
        else
        {
            var secret = new Secret(key, encryptedValue, tenantId, description);
            _context.Secrets.Add(secret);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Secret {Key} set for tenant {TenantId}", key, tenantId);
    }

    public async Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        var secret = await _context.Secrets
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

        if (secret == null) return null;

        return _encryptionService.Decrypt(secret.EncryptedValue);
    }

    public async Task<string?> ResolveAsync(string reference, CancellationToken cancellationToken = default)
    {
        // Expect format: {{secret:KEY}} or just KEY
        // If it starts with {{secret: and ends with }}, extract key.
        
        if (string.IsNullOrEmpty(reference)) return reference;

        string key = reference;
        if (reference.StartsWith("{{secret:") && reference.EndsWith("}}"))
        {
            key = reference.Substring(9, reference.Length - 11);
        }
        else if (!reference.StartsWith("{{secret:"))
        {
            // Not a secret reference, return as is (or maybe we only want to resolve strict references?)
            // Task implies "Replace raw secrets... with references".
            // So if I pass "password123", it's raw.
            // If I pass "{{secret:DB_PASSWORD}}", it should be resolved.
            // For safety, this method ONLY resolves if it detects the pattern.
            return reference;
        }

        var value = await GetSecretAsync(key, cancellationToken);
        if (value == null)
        {
            _logger.LogWarning("Secret reference {Reference} could not be resolved.", reference);
            return null; // Or return original reference? returning null signals failure.
        }

        return value;
    }
    
    public async Task<IEnumerable<object>> ListSecretsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Secrets
            .Select(s => new 
            {
                s.Id,
                s.Key,
                s.Description,
                s.LastModifiedOn,
                s.LastModifiedBy
            })
            .ToListAsync(cancellationToken);
    }
    
    public async Task DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        var secret = await _context.Secrets
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
            
        if (secret != null)
        {
            _context.Secrets.Remove(secret);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
