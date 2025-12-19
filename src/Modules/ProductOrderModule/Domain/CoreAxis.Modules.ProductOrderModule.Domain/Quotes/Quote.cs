using CoreAxis.SharedKernel;
using CoreAxis.Modules.ProductOrderModule.Domain.Enums;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;

namespace CoreAxis.Modules.ProductOrderModule.Domain.Quotes;

public class Quote : EntityBase
{
    public AssetCode AssetCode { get; private set; } = null!;
    public string ApplicationData { get; private set; } = string.Empty; // JSON
    public QuoteStatus Status { get; private set; }
    public DateTime ExpirationDate { get; private set; }
    public decimal? FinalPremium { get; private set; }
    public string ResultBlocks { get; private set; } = string.Empty; // JSON stored blocks

    private Quote() { }

    public static Quote Create(AssetCode assetCode, string applicationData, DateTime expirationDate)
    {
        return new Quote
        {
            AssetCode = assetCode,
            ApplicationData = applicationData,
            Status = QuoteStatus.Pending,
            ExpirationDate = expirationDate
        };
    }

    public void MarkAsReady(decimal finalPremium, string resultBlocks)
    {
        FinalPremium = finalPremium;
        ResultBlocks = resultBlocks;
        Status = QuoteStatus.Ready;
    }

    public void MarkAsConverted()
    {
        Status = QuoteStatus.Converted;
    }

    public void Expire()
    {
        Status = QuoteStatus.Expired;
    }
}
