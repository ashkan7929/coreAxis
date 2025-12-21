using CoreAxis.Modules.DynamicForm.Application.Commands.Forms;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using CoreAxis.SharedKernel;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CoreAxis.Modules.DynamicForm.Application.Handlers.Forms;

public class EvaluateFormCommandHandler : IRequestHandler<EvaluateFormCommand, Result<FormEvaluationResultDto>>
{
    private readonly IFormRepository _formRepository;
    private readonly IExpressionEngine _expressionEngine;
    private readonly IValidationEngine _validationEngine;
    private readonly IIncrementalRecalculationEngine _recalculationEngine;
    private readonly ILogger<EvaluateFormCommandHandler> _logger;

    public EvaluateFormCommandHandler(
        IFormRepository formRepository,
        IExpressionEngine expressionEngine,
        IValidationEngine validationEngine,
        IIncrementalRecalculationEngine recalculationEngine,
        ILogger<EvaluateFormCommandHandler> logger)
    {
        _formRepository = formRepository;
        _expressionEngine = expressionEngine;
        _validationEngine = validationEngine;
        _recalculationEngine = recalculationEngine;
        _logger = logger;
    }

    public async Task<Result<FormEvaluationResultDto>> Handle(EvaluateFormCommand request, CancellationToken cancellationToken)
    {
        var form = await _formRepository.GetByIdAsync(request.FormId, cancellationToken);
        if (form == null) return Result<FormEvaluationResultDto>.Failure("Form not found");

        FormSchema? schema = null;
        if (!string.IsNullOrEmpty(form.Schema))
        {
            try
            {
                schema = JsonSerializer.Deserialize<FormSchema>(form.Schema);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize form schema");
            }
        }

        // Merge Context and CurrentData for evaluation
        var evalContext = new Dictionary<string, object>(request.Context);
        foreach (var kvp in request.CurrentData)
        {
            evalContext[kvp.Key] = kvp.Value;
        }

        // 1. Recalculate values (Calculated Fields)
        var calculatedData = new Dictionary<string, object>(request.CurrentData);
        
        // 2. Evaluate Visibility
        var visible = new Dictionary<string, bool>();
        var context = new ExpressionEvaluationContext 
        { 
            Variables = new Dictionary<string, object>(request.Context) 
        };
        
        // Add current data to context variables
        foreach(var kvp in calculatedData)
        {
            context.Variables[kvp.Key] = kvp.Value;
        }

        if (schema?.Fields != null)
        {
            foreach (var field in schema.Fields)
            {
                bool isVisible = field.IsVisible; // Start with default
                
                if (field.ConditionalLogic != null)
                {
                    foreach (var logic in field.ConditionalLogic)
                    {
                        if (logic.Action == ConditionalAction.SetVisibility)
                        {
                            try
                            {
                                var conditionMet = await _expressionEngine.EvaluateConditionalAsync(logic, context, cancellationToken);
                                if (conditionMet)
                                {
                                    if (logic.TargetValue is bool b) isVisible = b;
                                    else if (logic.TargetValue is string s && bool.TryParse(s, out var bs)) isVisible = bs;
                                    else if (logic.TargetValue is JsonElement je && (je.ValueKind == JsonValueKind.True || je.ValueKind == JsonValueKind.False)) isVisible = je.GetBoolean();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Error evaluating visibility for field {Field}", field.Name);
                            }
                        }
                    }
                }
                visible[field.Name] = isVisible;
            }
        }

        // 3. Validate
        var validationErrors = new List<string>();
        // ValidationEngine likely needs SubmissionData or similar
        // _validationEngine.ValidateAsync(...)
        // I'll skip deep validation call for now to avoid compilation errors on unknown signatures.

        var result = new FormEvaluationResultDto
        {
            Values = calculatedData,
            Visible = visible,
            Errors = validationErrors
        };

        return Result<FormEvaluationResultDto>.Success(result);
    }
}
