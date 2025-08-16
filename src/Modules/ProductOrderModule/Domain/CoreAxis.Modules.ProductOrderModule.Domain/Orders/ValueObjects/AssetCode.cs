using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Orders.ValueObjects;

public class AssetCode : ValueObject
{
    public string Value { get; private set; }

    private AssetCode() { } // For EF Core

    private AssetCode(string value)
    {
        Value = value;
    }

    public static AssetCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("AssetCode cannot be null or empty", nameof(value));
        
        if (value.Length < 2 || value.Length > 10)
            throw new ArgumentException("AssetCode must be between 2 and 10 characters", nameof(value));
        
        return new AssetCode(value.ToUpperInvariant());
    }

    public static implicit operator string(AssetCode assetCode) => assetCode.Value;
    public static implicit operator AssetCode(string value) => Create(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}