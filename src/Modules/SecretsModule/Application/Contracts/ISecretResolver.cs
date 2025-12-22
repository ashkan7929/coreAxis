namespace CoreAxis.Modules.SecretsModule.Application.Contracts;

public interface ISecretResolver
{
    Task<string?> ResolveAsync(string reference, CancellationToken cancellationToken = default);
}
