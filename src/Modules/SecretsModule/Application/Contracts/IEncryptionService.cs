namespace CoreAxis.Modules.SecretsModule.Application.Contracts;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
