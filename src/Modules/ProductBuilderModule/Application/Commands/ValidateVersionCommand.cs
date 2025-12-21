using CoreAxis.Modules.ProductBuilderModule.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Ports;
using MediatR;

namespace CoreAxis.Modules.ProductBuilderModule.Application.Commands;

public record ValidateVersionCommand(Guid VersionId) : IRequest<Result<List<string>>>;

public class ValidateVersionCommandHandler : IRequestHandler<ValidateVersionCommand, Result<List<string>>>
{
    private readonly IProductRepository _repository;
    private readonly IWorkflowDefinitionClient _workflowClient;
    private readonly IFormClient _formClient;
    private readonly IMappingClient _mappingClient;
    private readonly IFormulaClient _formulaClient;

    public ValidateVersionCommandHandler(
        IProductRepository repository,
        IWorkflowDefinitionClient workflowClient,
        IFormClient formClient,
        IMappingClient mappingClient,
        IFormulaClient formulaClient
        )
    {
        _repository = repository;
        _workflowClient = workflowClient;
        _formClient = formClient;
        _mappingClient = mappingClient;
        _formulaClient = formulaClient;
    }

    public async Task<Result<List<string>>> Handle(ValidateVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetVersionAsync(request.VersionId, cancellationToken);
        if (version == null) return Result<List<string>>.Failure("Version not found");

        var errors = new List<string>();

        if (version.Binding == null)
        {
            errors.Add("Version has no bindings.");
            return Result<List<string>>.Success(errors);
        }

        var binding = version.Binding;

        // 1. Validate Workflow
        if (string.IsNullOrEmpty(binding.WorkflowDefinitionCode))
        {
            errors.Add("Workflow definition is required.");
        }
        else
        {
            int? wfVersion = null;
            if (!string.IsNullOrEmpty(binding.WorkflowVersionNumber) && int.TryParse(binding.WorkflowVersionNumber, out int v))
            {
                wfVersion = v;
            }

            if (wfVersion.HasValue)
            {
                 bool isPublished = await _workflowClient.IsWorkflowDefinitionPublishedAsync(binding.WorkflowDefinitionCode, wfVersion.Value, cancellationToken);
                 if (!isPublished)
                 {
                     errors.Add($"Workflow {binding.WorkflowDefinitionCode} v{wfVersion} is not published or does not exist.");
                 }
            }
            else
            {
                 // Check if definition exists at all
                 bool exists = await _workflowClient.WorkflowDefinitionExistsAsync(binding.WorkflowDefinitionCode, null, cancellationToken);
                 if (!exists)
                 {
                     errors.Add($"Workflow {binding.WorkflowDefinitionCode} does not exist.");
                 }
                 else
                 {
                     if (string.IsNullOrEmpty(binding.WorkflowVersionNumber))
                        errors.Add("Workflow version must be pinned.");
                 }
            }
        }

        // 2. Validate Form
        if (binding.InitialFormId.HasValue)
        {
            if (string.IsNullOrEmpty(binding.InitialFormVersion))
            {
                errors.Add("Initial Form version must be pinned.");
            }
            else
            {
                bool isPublished = await _formClient.IsFormPublishedAsync(binding.InitialFormId.Value, binding.InitialFormVersion, cancellationToken);
                if (!isPublished)
                {
                    errors.Add($"Form {binding.InitialFormId} v{binding.InitialFormVersion} is not published or does not exist.");
                }
            }
        }

        // 3. Validate Mapping
        if (binding.MappingSetId.HasValue)
        {
             bool exists = await _mappingClient.IsMappingSetPublishedAsync(binding.MappingSetId.Value, cancellationToken);
             if (!exists)
             {
                 errors.Add($"MappingSet {binding.MappingSetId} does not exist or is not active.");
             }
        }

        // 4. Validate Formula
        if (binding.FormulaId.HasValue)
        {
            if (string.IsNullOrEmpty(binding.FormulaVersion))
            {
                errors.Add("Formula version must be pinned.");
            }
            else
            {
                bool isPublished = await _formulaClient.IsFormulaPublishedAsync(binding.FormulaId.Value, binding.FormulaVersion, cancellationToken);
                if (!isPublished)
                {
                    errors.Add($"Formula {binding.FormulaId} v{binding.FormulaVersion} is not published or does not exist.");
                }
            }
        }

        return Result<List<string>>.Success(errors);
    }
}
