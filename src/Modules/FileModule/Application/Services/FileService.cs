using CoreAxis.Modules.FileModule.Application.Contracts;
using CoreAxis.Modules.FileModule.Domain.Entities;
using CoreAxis.Modules.FileModule.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreAxis.Modules.FileModule.Application.Services;

public class FileService
{
    private readonly FileDbContext _context;
    private readonly IFileStorageProvider _storageProvider;
    private readonly ILogger<FileService> _logger;

    public FileService(
        FileDbContext context,
        IFileStorageProvider storageProvider,
        ILogger<FileService> logger)
    {
        _context = context;
        _storageProvider = storageProvider;
        _logger = logger;
    }

    public async Task<FileMetadata> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long sizeBytes,
        string tenantId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var storagePath = await _storageProvider.SaveFileAsync(fileStream, fileName, cancellationToken);

        var metadata = new FileMetadata
        {
            FileName = fileName,
            ContentType = contentType,
            SizeBytes = sizeBytes,
            StorageProvider = _storageProvider.ProviderName,
            StoragePath = storagePath,
            TenantId = tenantId,
            UploadedByUserId = userId,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = userId ?? "System",
            LastModifiedBy = userId ?? "System",
            LastModifiedOn = DateTime.UtcNow
        };

        _context.Files.Add(metadata);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File uploaded successfully. Id: {FileId}, Name: {FileName}", metadata.Id, fileName);

        return metadata;
    }

    public async Task<(Stream? Stream, FileMetadata? Metadata)> GetFileAsync(Guid fileId, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var metadata = await _context.Files.FindAsync(new object[] { fileId }, cancellationToken);
        if (metadata == null)
        {
            return (null, null);
        }

        if (!string.IsNullOrEmpty(tenantId) && metadata.TenantId != tenantId)
        {
            _logger.LogWarning("Tenant mismatch for file {FileId}. Request tenant: {RequestTenant}, File tenant: {FileTenant}", 
                fileId, tenantId, metadata.TenantId);
            return (null, null); // Or throw UnauthorizedAccessException
        }

        var stream = await _storageProvider.GetFileStreamAsync(metadata.StoragePath, cancellationToken);
        return (stream, metadata);
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        return await _context.Files.FindAsync(new object[] { fileId }, cancellationToken);
    }
}
