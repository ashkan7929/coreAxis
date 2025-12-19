namespace CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Connectors;

public interface IFanavaranConnector
{
    Task<string> CreateCustomerAsync(string customerData, CancellationToken cancellationToken);
    Task<string> IssuePolicyAsync(string policyData, CancellationToken cancellationToken);
    Task<decimal> GetUniversalLifePriceAsync(string customerId, string applicationData, CancellationToken cancellationToken);
}
