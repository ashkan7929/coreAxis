namespace CoreAxis.Modules.ProductBuilderModule.Application.DTOs;

public class CreateVersionDto
{
    public string VersionNumber { get; set; } = default!;
    public string? Changelog { get; set; }
}
