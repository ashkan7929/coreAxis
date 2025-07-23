namespace CoreAxis.Modules.AuthModule.Application.DTOs;

public class PageDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePageDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class UpdatePageDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Path { get; set; }
    public bool? IsActive { get; set; }
    public int? SortOrder { get; set; }
}