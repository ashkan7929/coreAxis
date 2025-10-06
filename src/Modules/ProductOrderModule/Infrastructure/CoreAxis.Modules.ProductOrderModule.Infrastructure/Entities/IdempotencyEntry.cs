using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreAxis.Modules.ProductOrderModule.Infrastructure.Entities;

[Table("IdempotencyEntries", Schema = "productorder")]
public class IdempotencyEntry
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Operation { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string RequestHash { get; set; } = string.Empty;

    public DateTime CreatedOn { get; set; }
}