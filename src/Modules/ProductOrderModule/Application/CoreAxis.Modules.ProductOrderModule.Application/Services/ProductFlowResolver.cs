using CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Flow;
using CoreAxis.Modules.ProductOrderModule.Application.Flows.Mehraban;
using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace CoreAxis.Modules.ProductOrderModule.Application.Services;

public class ProductFlowResolver : IProductFlowResolver
{
    private readonly IServiceProvider _serviceProvider;

    public ProductFlowResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IQuoteFlow ResolveQuoteFlow(AssetCode assetCode)
    {
        if (assetCode.Value == "1456")
        {
            return ActivatorUtilities.CreateInstance<MehrabanQuoteFlow>(_serviceProvider);
        }
        throw new NotImplementedException($"Quote flow for asset code {assetCode.Value} not implemented.");
    }

    public IPostPaymentFlow ResolvePostPaymentFlow(AssetCode assetCode)
    {
        // For MVP, just return null or throw if not needed yet
        throw new NotImplementedException($"Post payment flow for asset code {assetCode.Value} not implemented.");
    }
}
