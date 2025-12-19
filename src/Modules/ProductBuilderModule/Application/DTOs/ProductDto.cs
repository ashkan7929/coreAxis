namespace CoreAxis.Modules.ProductBuilderModule.Application.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsActive { get; set; }
}
