using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Domain;
using System.ComponentModel.DataAnnotations;

namespace CoreAxis.Modules.FileModule.Domain.Entities;

public class FileMetadata : EntityBase, IMustHaveTenant
{
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }

    [Required]
    [MaxLength(50)]
    public string StorageProvider { get; set; } = "Local";

    [Required]
    [MaxLength(500)]
    public string StoragePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? UploadedByUserId { get; set; }

    public bool IsTemporary { get; set; } = false;

    [MaxLength(500)]
    public string? ContentHash { get; set; }
}
