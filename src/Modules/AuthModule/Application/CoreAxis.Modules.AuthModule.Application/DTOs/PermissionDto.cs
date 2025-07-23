namespace CoreAxis.Modules.AuthModule.Application.DTOs;

public class PermissionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public PageDto Page { get; set; } = new();
    public ActionDto Action { get; set; } = new();
}

public class CreatePermissionDto
{
    public Guid PageId { get; set; }
    public Guid ActionId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class UpdatePermissionDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class AssignPermissionDto
{
    public Guid UserId { get; set; }
    public Guid PermissionId { get; set; }
    public bool IsGranted { get; set; } = true;
}