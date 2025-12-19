using CoreAxis.Modules.ProductOrderModule.Domain.Quotes;
using CoreAxis.Modules.ProductOrderModule.Domain.Entities;

namespace CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Flow;

public interface IPostPaymentFlow
{
    Task ProcessPostPaymentAsync(Quote quote, Order order, CancellationToken cancellationToken);
}
