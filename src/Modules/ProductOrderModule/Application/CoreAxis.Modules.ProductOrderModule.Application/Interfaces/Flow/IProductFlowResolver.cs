using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;

namespace CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Flow;

public interface IProductFlowResolver
{
    IQuoteFlow ResolveQuoteFlow(AssetCode assetCode);
    IPostPaymentFlow ResolvePostPaymentFlow(AssetCode assetCode);
}
