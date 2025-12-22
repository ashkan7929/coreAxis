using CoreAxis.Modules.FileModule.Application.Contracts;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.FileModule.Infrastructure.Providers;

public class LocalFileStorageProvider : IFileStorageProvider
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageProvider> _logger;

    public string ProviderName => "Local";

    public LocalFileStorageProvider(string basePath, ILogger<LocalFileStorageProvider> logger)
    {
        _basePath = basePath;
        _logger = logger;
        
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var fullPath = Path.Combine(_basePath, uniqueFileName);

        using var destStream = new FileStream(fullPath, FileMode.Create);
        await fileStream.CopyToAsync(destStream, cancellationToken);

        return uniqueFileName; // Return relative path (filename)
    }

    public Task<Stream?> GetFileStreamAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("File not found at {Path}", fullPath);
            return Task.FromResult<Stream?>(null);
        }

        return Task.FromResult<Stream?>(new FileStream(fullPath, FileMode.Open, FileAccess.Read));
    }

    public Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }
}
