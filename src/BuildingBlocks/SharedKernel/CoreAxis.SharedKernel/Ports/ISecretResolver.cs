namespace CoreAxis.SharedKernel.Ports;

public interface ISecretResolver
{
    Task<string?> ResolveAsync(string reference, CancellationToken cancellationToken = default);
}
