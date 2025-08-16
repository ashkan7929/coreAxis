using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;

/// <summary>
/// Value object representing an asset code (e.g., BTC, ETH, USD).
/// </summary>
public class AssetCode : ValueObject
{
    public string Value { get; private set; }

    private AssetCode(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new AssetCode instance.
    /// </summary>
    /// <param name="value">The asset code value (e.g., "BTC", "ETH")</param>
    /// <returns>A new AssetCode instance</returns>
    /// <exception cref="ArgumentException">Thrown when the asset code is invalid</exception>
    public static AssetCode Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Asset code cannot be null or empty.", nameof(value));

        if (value.Length < 2 || value.Length > 10)
            throw new ArgumentException("Asset code must be between 2 and 10 characters.", nameof(value));

        if (!value.All(char.IsLetterOrDigit))
            throw new ArgumentException("Asset code can only contain letters and digits.", nameof(value));

        return new AssetCode(value.ToUpperInvariant());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(AssetCode assetCode) => assetCode.Value;
}