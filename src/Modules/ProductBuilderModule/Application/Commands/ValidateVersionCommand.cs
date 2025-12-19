using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using MediatR;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Commands;

public record ValidateVersionCommand(Guid VersionId) : IRequest<Result<List<string>>>;

public class ValidateVersionCommandHandler : IRequestHandler<ValidateVersionCommand, Result<List<string>>>
{
    private readonly IProductRepository _repository;

    public ValidateVersionCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<string>>> Handle(ValidateVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionAsync(request.VersionId, cancellationToken);
        if (version == null) return Result<List<string>>.Failure("Version not found");

        var errors = new List<string>();

        // Basic validation logic
        if (version.Binding == null)
        {
            errors.Add("Version has no bindings.");
        }
        else
        {
            // In a real implementation, we would call other modules to verify existence of IDs
            // e.g. Workflow, Form, Mapping, etc.
            // For now, we assume valid if fields are populated where required.
            
            // Example check
            if (string.IsNullOrEmpty(version.Binding.WorkflowDefinitionCode))
                errors.Add("Workflow definition is required.");
        }

        return Result<List<string>>.Success(errors);
    }
}
