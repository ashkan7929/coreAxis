using CoreAxis.Modules.Workflow.Application.DTOs.DSL;
using CoreAxis.SharedKernel;
using System.Text.Json;

namespace CoreAxis.Modules.Workflow.Application.Services;

public class WorkflowValidator : IWorkflowValidator
{
    private readonly IWorkflowStepRegistry _stepRegistry;

    public WorkflowValidator(IWorkflowStepRegistry stepRegistry)
    {
        _stepRegistry = stepRegistry;
    }

    public Task<Result<bool>> ValidateDslAsync(string dslJson)
    {
        WorkflowDsl? dsl;
        try
        {
            dsl = JsonSerializer.Deserialize<WorkflowDsl>(dslJson);
        }
        catch (JsonException ex)
        {
            return Task.FromResult(Result<bool>.Failure($"Invalid JSON format: {ex.Message}"));
        }

        if (dsl == null)
            return Task.FromResult(Result<bool>.Failure("DSL cannot be null"));

        var errors = new List<string>();

        // 1. Check StartAt
        if (string.IsNullOrWhiteSpace(dsl.StartAt))
        {
            errors.Add("Workflow must have a 'startAt' property defined.");
        }

        // 2. Check Steps existence
        if (dsl.Steps == null || !dsl.Steps.Any())
        {
            errors.Add("Workflow must have at least one step.");
            return Task.FromResult(Result<bool>.Failure(errors));
        }

        var stepIds = dsl.Steps.Select(s => s.Id).ToHashSet();
        if (dsl.Steps.Count != stepIds.Count)
        {
            errors.Add("Duplicate step IDs found.");
        }

        if (!string.IsNullOrWhiteSpace(dsl.StartAt) && !stepIds.Contains(dsl.StartAt))
        {
            errors.Add($"Start step '{dsl.StartAt}' not found in steps list.");
        }

        // 3. Validate each step
        foreach (var step in dsl.Steps)
        {
            if (string.IsNullOrWhiteSpace(step.Id))
                errors.Add("A step is missing an ID.");

            if (string.IsNullOrWhiteSpace(step.Type))
            {
                errors.Add($"Step '{step.Id}' is missing a type.");
            }
            else
            {
                var stepType = _stepRegistry.GetStepType(step.Type);
                if (stepType == null)
                {
                    errors.Add($"Unknown step type '{step.Type}' in step '{step.Id}'.");
                }
            }

            // 4. Validate transitions
            if (step.Transitions != null)
            {
                foreach (var transition in step.Transitions)
                {
                    if (string.IsNullOrWhiteSpace(transition.To))
                    {
                        errors.Add($"Step '{step.Id}' has a transition with empty target.");
                    }
                    else if (!stepIds.Contains(transition.To))
                    {
                        errors.Add($"Step '{step.Id}' transitions to unknown step '{transition.To}'.");
                    }
                }
            }
        }

        if (errors.Any())
        {
            return Task.FromResult(Result<bool>.Failure(errors));
        }

        return Task.FromResult(Result<bool>.Success(true));
    }
}
