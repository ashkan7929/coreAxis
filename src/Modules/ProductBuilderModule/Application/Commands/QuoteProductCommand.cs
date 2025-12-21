using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Ports;
using MediatR;
using System.Text.Json;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Commands;

public record QuoteProductCommand(string ProductKey, JsonElement Inputs) : IRequest<Result<Dictionary<string, object>>>;

public class QuoteProductCommandHandler : IRequestHandler<QuoteProductCommand, Result<Dictionary<string, object>>>
{
    private readonly IProductRepository _repository;
    private readonly IFormulaClient _formulaClient;

    public QuoteProductCommandHandler(IProductRepository repository, IFormulaClient formulaClient)
    {
        _repository = repository;
        _formulaClient = formulaClient;
    }

    public async Task<Result<Dictionary<string, object>>> Handle(QuoteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByKeyAsync(request.ProductKey, cancellationToken);
        if (product == null)
        {
            return Result<Dictionary<string, object>>.Failure($"Product not found: {request.ProductKey}");
        }

        var version = await _repository.GetPublishedVersionAsync(product.Id, cancellationToken);
        if (version == null)
        {
            return Result<Dictionary<string, object>>.Failure($"No published version found for product: {request.ProductKey}");
        }

        if (version.Binding == null || !version.Binding.FormulaId.HasValue)
        {
             return Result<Dictionary<string, object>>.Failure($"Product version {version.VersionNumber} has no formula binding");
        }

        if (string.IsNullOrEmpty(version.Binding.FormulaVersion))
        {
            return Result<Dictionary<string, object>>.Failure("Formula version is not pinned in binding");
        }

        var inputs = JsonSerializer.Deserialize<Dictionary<string, object>>(request.Inputs.GetRawText()) ?? new Dictionary<string, object>();

        // Add product context
        inputs["product"] = new 
        {
            key = product.Key,
            name = product.Name,
            version = version.VersionNumber
        };

        return await _formulaClient.EvaluateAsync(
            version.Binding.FormulaId.Value, 
            version.Binding.FormulaVersion, 
            inputs, 
            cancellationToken);
    }
}
