using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Entities;

public class Supplier : EntityBase
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private Supplier() { }

    public static Supplier Create(string code, string name)
    {
        var supplier = new Supplier
        {
            Code = code.Trim(),
            Name = name.Trim()
        };
        return supplier;
    }

    public void Update(string name, bool? isActive = null)
    {
        Name = name.Trim();
        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }
    }
}