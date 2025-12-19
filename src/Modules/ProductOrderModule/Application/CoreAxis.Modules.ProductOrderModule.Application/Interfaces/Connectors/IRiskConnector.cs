namespace CoreAxis.Modules.ProductOrderModule.Application.Interfaces.Connectors;

public interface IRiskConnector
{
    Task<decimal> CalculateRiskAsync(string healthData, CancellationToken cancellationToken);
}
