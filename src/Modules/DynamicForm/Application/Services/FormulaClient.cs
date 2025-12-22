using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.SharedKernel;
using CoreAxis.SharedKernel.Ports;

namespace CoreAxis.Modules.DynamicForm.Application.Services;

public class FormulaClient : IFormulaClient
{
    private readonly IFormulaDefinitionRepository _definitionRepository;
    private readonly IFormulaVersionRepository _versionRepository;
    private readonly IFormulaService _formulaService;

    public FormulaClient(
        IFormulaDefinitionRepository definitionRepository,
        IFormulaVersionRepository versionRepository,
        IFormulaService formulaService)
    {
        _definitionRepository = definitionRepository;
        _versionRepository = versionRepository;
        _formulaService = formulaService;
    }

    public async Task<bool> FormulaExistsAsync(Guid formulaId, string? version = null, CancellationToken cancellationToken = default)
    {
        var def = await _definitionRepository.GetByIdAsync(formulaId, cancellationToken);
        if (def == null) return false;

        if (!string.IsNullOrEmpty(version) && int.TryParse(version, out int v))
        {
             return (await _versionRepository.GetByFormulaDefinitionIdAndVersionAsync(formulaId, v, cancellationToken)) != null;
        }
        return true;
    }

    public async Task<bool> IsFormulaPublishedAsync(Guid formulaId, string version, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(version, out int v)) return false;
        var ver = await _versionRepository.GetByFormulaDefinitionIdAndVersionAsync(formulaId, v, cancellationToken);
        return ver != null && ver.IsPublished;
    }

    public async Task<Result<Dictionary<string, object>>> EvaluateAsync(Guid formulaId, string version, Dictionary<string, object> inputs, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(version, out int v))
        {
            return Result<Dictionary<string, object>>.Failure("Invalid version number");
        }

        var result = await _formulaService.EvaluateFormulaVersionAsync(formulaId, v, inputs, null, cancellationToken);
        
        if (!result.IsSuccess)
        {
            return Result<Dictionary<string, object>>.Failure(result.Error);
        }

        // Return dictionary with "value" and other metadata if needed
        var output = new Dictionary<string, object>
        {
            { "value", result.Value.Value ?? (object)"null" }
        };
        
        if (result.Value.Value != null)
        {
            output["resultType"] = result.Value.Value.GetType().Name;
        }
        
        // If the result is a dictionary/object, we might want to merge it?
        // But FormulaEvaluationResult.Result is object?.
        
        return Result<Dictionary<string, object>>.Success(output);
    }
}
