namespace CoreAxis.Modules.ProductOrderModule.Domain.Enums;

/// <summary>
/// Represents the type of order (buy or sell).
/// </summary>
public enum OrderType
{
    /// <summary>
    /// Buy order - purchasing an asset.
    /// </summary>
    Buy = 0,

    /// <summary>
    /// Sell order - selling an asset.
    /// </summary>
    Sell = 1
}