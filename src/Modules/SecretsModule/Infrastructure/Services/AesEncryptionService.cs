using CoreAxis.Modules.SecretsModule.Application.Contracts;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace CoreAxis.Modules.SecretsModule.Infrastructure.Services;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["Secrets:EncryptionKey"];
        // Ensure we have a valid key. In production this MUST be provided via secure config.
        // For development/demo, we'll generate a consistent one if missing or use a fallback.
        if (string.IsNullOrEmpty(keyString))
        {
            keyString = "DefaultInsecureKeyForDevelopmentOnly123!"; 
        }

        // AES-256 requires 32 bytes key
        using var sha256 = SHA256.Create();
        _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        
        // Use a fixed IV or derive it. Ideally IV should be random and stored with cipher.
        // For simplicity here, we'll derive a fixed IV from the key (less secure but functional for this scope).
        _iv = _key.Take(16).ToArray(); 
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);
        
        sw.Write(plainText);
        sw.Flush(); // Ensure all data is written to the CryptoStream
        cs.FlushFinalBlock(); // Flush the final block of encrypted data
        
        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}
