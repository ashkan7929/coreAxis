namespace CoreAxis.Modules.FileModule.Application.Contracts;

public interface IFileStorageProvider
{
    string ProviderName { get; }
    Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileStreamAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);
}
