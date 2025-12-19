using System.Text.Json;

namespace CoreAxis.Modules.MappingModule.Application.Services;

public interface ITransformEngine
{
    Task<string> ExecuteAsync(string rulesJson, string contextJson, CancellationToken cancellationToken = default);
    
    // For testing/validation
    Task<object?> EvaluateExpressionAsync(string expression, JsonElement context, CancellationToken cancellationToken = default);
}
