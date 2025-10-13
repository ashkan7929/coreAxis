using System;

namespace CoreAxis.Modules.Workflow.Domain.Entities;

public class IdempotencyKey
{
    public Guid Id { get; set; }
    public string Key { get; set; } = null!;
    public string Route { get; set; } = null!;
    public string BodyHash { get; set; } = null!;
    public string? ResponseJson { get; set; }
    public int StatusCode { get; set; }
    public DateTime CreatedAt { get; set; }
}