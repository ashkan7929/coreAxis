using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Implementation of dynamic options manager for form fields.
    /// Handles evaluation of dynamic options based on expressions and external data sources.
    /// </summary>
    public class DynamicOptionsManager : IDynamicOptionsManager
    {
        private readonly IExpressionEngine _expressionEngine;
        private readonly IApiProxy _apiProxy;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DynamicOptionsManager> _logger;

        public DynamicOptionsManager(
            IExpressionEngine expressionEngine,
            IApiProxy apiProxy,
            IMemoryCache cache,
            ILogger<DynamicOptionsManager> logger)
        {
            _expressionEngine = expressionEngine ?? throw new ArgumentNullException(nameof(expressionEngine));
            _apiProxy = apiProxy ?? throw new ArgumentNullException(nameof(apiProxy));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> EvaluateDynamicOptionsAsync(
            string expression,
            Dictionary<string, object?> formData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expression))
                {
                    return Failure<List<FieldOption>>("Expression cannot be null or empty");
                }

                _logger.LogDebug("Evaluating dynamic options expression: {Expression}", expression);

                // Check cache first
                var cacheKey = GenerateCacheKey(expression, formData);
                if (_cache.TryGetValue(cacheKey, out List<FieldOption>? cachedOptions) && cachedOptions != null)
                {
                    _logger.LogDebug("Returning cached options for expression: {Expression}", expression);
                    return Success(cachedOptions);
                }

                // Parse expression to determine the type of dynamic options
                var options = await EvaluateExpressionAsync(expression, formData, cancellationToken);
                if (options.IsFailure)
                {
                    return Failure<List<FieldOption>>(options.Error);
                }

                // Cache the results
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                };
                _cache.Set(cacheKey, options.Value, cacheOptions);

                _logger.LogDebug("Successfully evaluated dynamic options. Count: {Count}", options.Value?.Count ?? 0);
                return options;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating dynamic options expression: {Expression}", expression);
                return Failure<List<FieldOption>>($"Failed to evaluate dynamic options: {ex.Message}");
            }
        }

        public async Task<CoreAxis.SharedKernel.Result<Dictionary<string, List<FieldOption>>>> EvaluateMultipleDynamicOptionsAsync(
            Dictionary<string, string> expressions,
            Dictionary<string, object?> formData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var results = new Dictionary<string, List<FieldOption>>();
            var tasks = new List<Task<(string fieldName, Result<List<FieldOption>> result)>>();

            foreach (var kvp in expressions)
            {
                var fieldName = kvp.Key;
                var expression = kvp.Value;

                    tasks.Add(Task.Run(async () =>
                    {
                        var result = await EvaluateDynamicOptionsAsync(expression, formData, cancellationToken);
                        return (fieldName, result);
                    }, cancellationToken));
                }

                var completedTasks = await Task.WhenAll(tasks);

                foreach (var (fieldName, result) in completedTasks)
                {
                    if (result.IsSuccess)
                    {
                        results[fieldName] = result.Value!;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to evaluate dynamic options for field {FieldName}: {Error}", 
                            fieldName, result.Error);
                        results[fieldName] = new List<FieldOption>();
                    }
                }

                return Success(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating multiple dynamic options");
                return Failure<Dictionary<string, List<FieldOption>>>($"Failed to evaluate multiple dynamic options: {ex.Message}");
            }
        }

        public async Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> GetOptionsFromApiAsync(
            string apiUrl,
            Dictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting options from API endpoint: {Endpoint}", apiUrl);

                // Use ApiProxy to call the external API
                // Note: This needs to be updated to use the correct method signature for IApiProxy
                // var apiResult = await _apiProxy.InvokeAsync(methodId, parameters, cancellationToken);
                // For now, return a placeholder implementation
                await Task.Delay(100, cancellationToken); // Simulate API call
                
                var options = new List<FieldOption>
                {
                    FieldOption.Create("api_option_1", "API Option 1"),
                    FieldOption.Create("api_option_2", "API Option 2")
                };
                
                return Success(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting options from API: {Endpoint}", apiUrl);
                return Failure<List<FieldOption>>($"Failed to get options from API: {ex.Message}");
            }
        }

        public async Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> GetOptionsFromDatabaseAsync(
            string query,
            Dictionary<string, object?> parameters,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Getting options from database query: {Query}", query);

                // This would typically use a database service or repository
                // For now, return a placeholder implementation
                await Task.Delay(100, cancellationToken); // Simulate database call

                var options = new List<FieldOption>
                {
                    FieldOption.Create("db_option_1", "Database Option 1"),
                    FieldOption.Create("db_option_2", "Database Option 2")
                };

                return Success(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting options from database: {Query}", query);
                return Failure<List<FieldOption>>($"Failed to get options from database: {ex.Message}");
            }
        }

        public async Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> FilterOptionsAsync(
            List<FieldOption> options,
            string filterCondition,
            Dictionary<string, object?> formData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (options == null || !options.Any())
                {
                    return Success(new List<FieldOption>());
                }

                if (string.IsNullOrWhiteSpace(filterCondition))
                {
                    return Success(options);
                }

                _logger.LogDebug("Filtering options with expression: {Expression}", filterCondition);

                var filteredOptions = new List<FieldOption>();

                foreach (var option in options)
                {
                    // Create context for each option
                    var context = new Dictionary<string, object?>(formData)
                    {
                        ["option"] = new Dictionary<string, object?>
                        {
                            ["value"] = option.Value,
                            ["label"] = option.Label,
                            ["description"] = option.Description,
                            ["group"] = option.Group
                        }
                    };

                    var formulaExpression = FormulaExpression.Conditional(filterCondition);
                    var evaluationResult = await _expressionEngine.EvaluateAsync(formulaExpression, context, cancellationToken);
                    if (evaluationResult.IsSuccess && evaluationResult.Value is bool shouldInclude && shouldInclude)
                    {
                        filteredOptions.Add(option);
                    }
                }

                _logger.LogDebug("Filtered {OriginalCount} options to {FilteredCount}", options.Count, filteredOptions.Count);
                return Success(filteredOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering options with expression: {Expression}", filterCondition);
                return Failure<List<FieldOption>>($"Failed to filter options: {ex.Message}");
            }
        }

        public async Task<CoreAxis.SharedKernel.Result<bool>> ValidateDynamicOptionsExpression(string expression)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expression))
                {
                    return Failure<bool>("Expression cannot be null or empty");
                }

                // Use expression engine to validate syntax
                await Task.CompletedTask; // Make it async for consistency
                var validationResult = _expressionEngine.ValidateExpression(expression);
                if (validationResult.IsValid)
                {
                    return Success(true);
                }
                else
                {
                    var errorMessage = string.Join("; ", validationResult.Errors);
                    return Failure<bool>(errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating dynamic options expression: {Expression}", expression);
                return Failure<bool>($"Failed to validate expression: {ex.Message}");
            }
        }

        public async Task<CoreAxis.SharedKernel.Result<Dictionary<string, string>>> GetAvailableFunctions()
        {
            try
            {
                await Task.CompletedTask; // Make it async for consistency
                var functions = new Dictionary<string, string>
                {
                    ["api"] = "Fetch options from an external API endpoint",
                    ["database"] = "Fetch options from a database query",
                    ["filter"] = "Filter existing options based on conditions",
                    ["map"] = "Transform option values or labels",
                    ["sort"] = "Sort options by specified criteria",
                    ["group"] = "Group options by specified field",
                    ["limit"] = "Limit the number of options returned",
                    ["distinct"] = "Remove duplicate options",
                    ["conditional"] = "Show/hide options based on form data",
                    ["static"] = "Define static list of options",
                    ["if"] = "Conditional logic for dynamic options"
                };
                return Success(functions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available functions");
                return Failure<Dictionary<string, string>>($"Failed to get available functions: {ex.Message}");
            }
        }

        private async Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> EvaluateExpressionAsync(
            string expression, 
            Dictionary<string, object?> formData, 
            CancellationToken cancellationToken)
        {
            try
            {
                // Parse the expression to determine the operation type
                if (expression.StartsWith("api("))
                {
                    return await EvaluateApiExpression(expression, formData, cancellationToken);
                }
                else if (expression.StartsWith("database("))
                {
                    return await EvaluateDatabaseExpression(expression, formData, cancellationToken);
                }
                else if (expression.StartsWith("filter("))
                {
                    return await EvaluateFilterExpression(expression, formData, cancellationToken);
                }
                else if (expression.StartsWith("static("))
                {
                    return EvaluateStaticExpression(expression, formData);
                }
                else
                {
                    // Use expression engine for complex expressions
                    var result = await _expressionEngine.EvaluateAsync(expression, formData, cancellationToken);
                    if (result.IsFailure)
                {
                    return Failure<List<FieldOption>>(result.Error);
                }

                    // Convert result to field options
                    var options = ConvertToFieldOptions(result.Value);
                    return Success(options);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating expression: {Expression}", expression);
                return Failure<List<FieldOption>>($"Failed to evaluate expression: {ex.Message}");
            }
        }

        private async Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> EvaluateApiExpression(
            string expression, 
            Dictionary<string, object?> formData, 
            CancellationToken cancellationToken)
        {
            // Parse api(endpoint, parameters) expression
            // This is a simplified implementation
            var endpoint = ExtractApiEndpoint(expression);
            var parameters = ExtractApiParameters(expression, formData);
            
            return await GetOptionsFromApiAsync(endpoint, parameters, cancellationToken);
        }

        private async Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> EvaluateDatabaseExpression(
            string expression, 
            Dictionary<string, object?> formData, 
            CancellationToken cancellationToken)
        {
            // Parse database(query, parameters) expression
            var query = ExtractDatabaseQuery(expression);
            var parameters = ExtractDatabaseParameters(expression, formData);
            
            return await GetOptionsFromDatabaseAsync(query, parameters, cancellationToken);
        }

        private async Task<CoreAxis.SharedKernel.Result<List<FieldOption>>> EvaluateFilterExpression(
            string expression, 
            Dictionary<string, object?> formData, 
            CancellationToken cancellationToken)
        {
            // Parse filter(options, condition) expression
            var baseOptions = ExtractBaseOptions(expression);
            var filterCondition = ExtractFilterCondition(expression);
            
            return await FilterOptionsAsync(baseOptions, filterCondition, formData, cancellationToken);
        }

        private Result<List<FieldOption>> EvaluateStaticExpression(
            string expression, 
            Dictionary<string, object?> formData)
        {
            // Parse static([{value: "1", label: "Option 1"}, ...]) expression
            try
            {
                var optionsJson = ExtractStaticOptions(expression);
                var options = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(optionsJson);
                
                var fieldOptions = options?.Select(opt => FieldOption.Create(
                    opt.GetValueOrDefault("value", "").ToString() ?? "",
                    opt.GetValueOrDefault("label", "").ToString() ?? ""
                )).ToList() ?? new List<FieldOption>();

                return Success(fieldOptions);
            }
            catch (Exception ex)
            {
                return Failure<List<FieldOption>>($"Failed to parse static options: {ex.Message}");
            }
        }

        private List<FieldOption> ParseApiResponseToOptions(object apiResponse)
        {
            try
            {
                if (apiResponse is string jsonString)
                {
                    var items = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonString);
                    return items?.Select(item => FieldOption.Create(
                        item.GetValueOrDefault("value", "").ToString() ?? "",
                        item.GetValueOrDefault("label", "").ToString() ?? ""
                    )).ToList() ?? new List<FieldOption>();
                }

                return new List<FieldOption>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing API response to options");
                return new List<FieldOption>();
            }
        }

        private List<FieldOption> ConvertToFieldOptions(object? value)
        {
            if (value == null)
                return new List<FieldOption>();

            try
            {
                if (value is List<FieldOption> fieldOptions)
                    return fieldOptions;

                if (value is IEnumerable<object> enumerable)
                {
                    return enumerable.Select((item, index) => 
                        FieldOption.Create(item?.ToString() ?? "", item?.ToString() ?? ""))
                        .ToList();
                }

                return new List<FieldOption> { FieldOption.Create(value.ToString() ?? "", value.ToString() ?? "") };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting value to field options");
                return new List<FieldOption>();
            }
        }

        private string GenerateCacheKey(string expression, Dictionary<string, object?> formData)
        {
            var dataHash = JsonSerializer.Serialize(formData).GetHashCode();
            return $"dynamic_options_{expression.GetHashCode()}_{dataHash}";
        }

        // Helper methods for parsing expressions (simplified implementations)
        private string ExtractApiEndpoint(string expression) => "https://api.example.com/options";
        private Dictionary<string, object?> ExtractApiParameters(string expression, Dictionary<string, object?> formData) => new();
        private string ExtractDatabaseQuery(string expression) => "SELECT value, label FROM options";
        private Dictionary<string, object?> ExtractDatabaseParameters(string expression, Dictionary<string, object?> formData) => new();
        private List<FieldOption> ExtractBaseOptions(string expression) => new();
        private string ExtractFilterCondition(string expression) => "true";
        private string ExtractStaticOptions(string expression) => "[]";
    }
}