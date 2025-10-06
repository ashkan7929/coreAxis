using System;
using System.Security.Cryptography;
using System.Text;

namespace CoreAxis.Modules.ApiManager.Application.Security;

public interface IHmacCanonicalSigner
{
    string ComputeSignature(
        string httpMethod,
        string path,
        string query,
        string timestamp,
        string? bodyHash,
        string secret,
        string algorithm);

    string ComputeBodyHash(string content, string algorithm);
}

public sealed class HmacCanonicalSigner : IHmacCanonicalSigner
{
    public string ComputeSignature(
        string httpMethod,
        string path,
        string query,
        string timestamp,
        string? bodyHash,
        string secret,
        string algorithm)
    {
        var canonical = string.Join("\n",
            httpMethod.ToUpperInvariant(),
            string.IsNullOrEmpty(path) ? "/" : path,
            query ?? string.Empty,
            timestamp ?? string.Empty,
            bodyHash ?? string.Empty);

        var key = Encoding.UTF8.GetBytes(secret ?? string.Empty);
        using var hmac = CreateHmac(algorithm, key);
        var bytes = Encoding.UTF8.GetBytes(canonical);
        var sigBytes = hmac.ComputeHash(bytes);
        return Convert.ToHexString(sigBytes).ToLowerInvariant();
    }

    public string ComputeBodyHash(string content, string algorithm)
    {
        var bytes = Encoding.UTF8.GetBytes(content ?? string.Empty);
        using HashAlgorithm hash = algorithm.ToUpperInvariant() switch
        {
            "HMACSHA1" => SHA1.Create(),
            "HMACSHA256" => SHA256.Create(),
            "HMACSHA384" => SHA384.Create(),
            "HMACSHA512" => SHA512.Create(),
            _ => SHA256.Create()
        };
        var hashed = hash.ComputeHash(bytes);
        return Convert.ToHexString(hashed).ToLowerInvariant();
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