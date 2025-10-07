using CoreAxis.Modules.ApiManager.Application.Contracts;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;
using FieldOption = CoreAxis.Modules.DynamicForm.Domain.ValueObjects.FieldOption;
using CoreAxis.SharedKernel;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;


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

        public async Task<Result<List<FieldOption>>> EvaluateDynamicOptionsAsync(
            string expression,
            Dictionary<string, object?> context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expression))
                {
                    return Result.Failure<List<FieldOption>>("Expression cannot be null or empty");
                }

                _logger.LogDebug("Evaluating dynamic options expression: {Expression}", expression);

                // Check cache first
                var cacheKey = GenerateCacheKey(expression, context);
                if (_cache.TryGetValue(cacheKey, out List<FieldOption>? cachedOptions) && cachedOptions != null)
                {
                    _logger.LogDebug("Returning cached options for expression: {Expression}", expression);
                    return Result.Success(cachedOptions);
                }

                // Parse expression to determine the type of dynamic options
                var options = await EvaluateExpressionAsync(expression, context, cancellationToken);
                if (options.IsFailure)
                {
                    return Result.Failure<List<FieldOption>>(options.Error);
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
                return Result.Failure<List<FieldOption>>($"Failed to evaluate dynamic options: {ex.Message}");
            }
        }

        public async Task<Result<Dictionary<string, List<FieldOption>>>> EvaluateMultipleDynamicOptionsAsync(
            Dictionary<string, string> expressions,
            Dictionary<string, object?> context,
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
                        var result = await EvaluateDynamicOptionsAsync(expression, context, cancellationToken);
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

                return Result.Success(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating multiple dynamic options");
                return Result.Failure<Dictionary<string, List<FieldOption>>>($"Failed to evaluate multiple dynamic options: {ex.Message}");
            }
        }

        public async Task<Result<List<FieldOption>>> GetOptionsFromApiAsync(
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
                
                return Result.Success(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting options from API: {Endpoint}", apiUrl);
                return Result.Failure<List<FieldOption>>($"Failed to get options from API: {ex.Message}");
            }
        }

        public async Task<Result<List<FieldOption>>> GetOptionsFromDatabaseAsync(
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

                return Result.Success(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting options from database: {Query}", query);
                return Result.Failure<List<FieldOption>>($"Failed to get options from database: {ex.Message}");
            }
        }

        public async Task<Result<List<FieldOption>>> FilterOptionsAsync(
        List<FieldOption> options,
        string filterExpression,
        Dictionary<string, object?> context,
        CancellationToken cancellationToken = default)
        {
            try
            {
                if (options == null || !options.Any())
                {
                    return Result.Success(new List<FieldOption>());
                }

                if (string.IsNullOrWhiteSpace(filterExpression))
                {
                    return Result.Success(options);
                }

                _logger.LogDebug("Filtering options with expression: {Expression}", filterExpression);

                var filteredOptions = new List<FieldOption>();

                foreach (var option in options)
                {
                    // Create context for each option
                    var optionContext = new Dictionary<string, object?>(context)
                    {
                        ["option"] = new Dictionary<string, object?>
                        {
                            ["value"] = option.Value,
                            ["label"] = option.Label,
                            ["description"] = option.Description,
                            ["group"] = option.Group
                        }
                    };

                    var formulaExpression = FormulaExpression.Conditional(filterExpression);
                    var evaluationResult = await _expressionEngine.EvaluateAsync(formulaExpression, optionContext, cancellationToken);
                    if (evaluationResult.IsSuccess && evaluationResult.Value is bool shouldInclude && shouldInclude)
                    {
                        filteredOptions.Add(option);
                    }
                }

                _logger.LogDebug("Filtered {OriginalCount} options to {FilteredCount}", options.Count, filteredOptions.Count);
                return Result.Success(filteredOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error filtering options with expression: {Expression}", filterExpression);
                return Result.Failure<List<FieldOption>>($"Failed to filter options: {ex.Message}");
            }
        }

        public async Task<Result<bool>> ValidateDynamicOptionsExpression(
            string expression,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expression))
                {
                    return Result.Failure<bool>("Expression cannot be null or empty");
                }

                // Basic validation - check for common patterns
                var validPatterns = new[] { "static(", "api(", "database(", "filter(", "if(" };
                var hasValidPattern = validPatterns.Any(pattern => expression.Contains(pattern));
                
                if (!hasValidPattern)
                {
                    return Result.Failure<bool>("Expression does not contain a valid function pattern");
                }

                // Additional validation could be added here
                await Task.CompletedTask;
                return Result.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating dynamic options expression: {Expression}", expression);
                return Result.Failure<bool>($"Failed to validate expression: {ex.Message}");
            }
        }

        public async Task<Result<Dictionary<string, string>>> GetAvailableFunctions()
        {
            try
            {
                var functions = new Dictionary<string, string>
                {
                    ["static"] = "Returns a static list of options defined in the expression",
                    ["api"] = "Fetches options from an external API endpoint",
                    ["database"] = "Retrieves options from a database query",
                    ["filter"] = "Filters existing options based on conditions",
                    ["if"] = "Conditional expression to return different options based on form data"
                };
                
                await Task.CompletedTask;
                return Result.Success(functions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available functions");
                return Result.Failure<Dictionary<string, string>>($"Failed to get available functions: {ex.Message}");
            }
        }

        private async Task<Result<List<FieldOption>>> EvaluateExpressionAsync(
            string expression, 
            Dictionary<string, object?> context, 
            CancellationToken cancellationToken)
        {
            try
            {
                // Parse the expression to determine the operation type
                if (expression.StartsWith("api("))
                {
                    return await EvaluateApiExpression(expression, context, cancellationToken);
                }
                else if (expression.StartsWith("database("))
                {
                    return await EvaluateDatabaseExpression(expression, context, cancellationToken);
                }
                else if (expression.StartsWith("filter("))
                {
                    return await EvaluateFilterExpression(expression, context, cancellationToken);
                }
                else if (expression.StartsWith("static("))
                {
                    return EvaluateStaticExpression(expression, context);
                }
                else
                {
                    // Use expression engine for complex expressions
                    var result = await _expressionEngine.EvaluateAsync(expression, context, cancellationToken);
                    if (result.IsFailure)
                {
                    return Result.Failure<List<FieldOption>>(result.Error);
                }

                    // Convert result to field options
                    var options = ConvertToFieldOptions(result.Value);
                    return Result.Success(options);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating expression: {Expression}", expression);
                return Result.Failure<List<FieldOption>>($"Failed to evaluate expression: {ex.Message}");
            }
        }

        private async Task<Result<List<FieldOption>>> EvaluateApiExpression(
            string expression, 
            Dictionary<string, object?> context, 
            CancellationToken cancellationToken)
        {
            // Parse api(endpoint, parameters) expression
            // This is a simplified implementation
            var endpoint = ExtractApiEndpoint(expression);
            var parameters = ExtractApiParameters(expression, context);
            
            return await GetOptionsFromApiAsync(endpoint, parameters, cancellationToken);
        }

        private async Task<Result<List<FieldOption>>> EvaluateDatabaseExpression(
            string expression, 
            Dictionary<string, object?> context, 
            CancellationToken cancellationToken)
        {
            // Parse database(query, parameters) expression
            var query = ExtractDatabaseQuery(expression);
            var parameters = ExtractDatabaseParameters(expression, context);
            
            return await GetOptionsFromDatabaseAsync(query, parameters, cancellationToken);
        }

        private async Task<Result<List<FieldOption>>> EvaluateFilterExpression(
            string expression, 
            Dictionary<string, object?> context, 
            CancellationToken cancellationToken)
        {
            // Parse filter(options, condition) expression
            var baseOptions = ExtractBaseOptions(expression);
            var filterCondition = ExtractFilterCondition(expression);
            
            return await FilterOptionsAsync(baseOptions, filterCondition, context, cancellationToken);
        }

        private Result<List<FieldOption>> EvaluateStaticExpression(
            string expression, 
            Dictionary<string, object?> context)
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

                return Result.Success(fieldOptions);
             }
             catch (Exception ex)
             {
                 return Result.Failure<List<FieldOption>>($"Failed to parse static options: {ex.Message}");
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

        private string GenerateCacheKey(string expression, Dictionary<string, object?> context)
        {
            var dataHash = JsonSerializer.Serialize(context).GetHashCode();
            return $"dynamic_options_{expression.GetHashCode()}_{dataHash}";
        }

        // Helper methods for parsing expressions (simplified implementations)
        private string ExtractApiEndpoint(string expression) => "https://api.example.com/options";
        private Dictionary<string, object?> ExtractApiParameters(string expression, Dictionary<string, object?> context) => new();
        private string ExtractDatabaseQuery(string expression) => "SELECT value, label FROM options";
        private Dictionary<string, object?> ExtractDatabaseParameters(string expression, Dictionary<string, object?> context) => new();
        private List<FieldOption> ExtractBaseOptions(string expression) => new();
        private string ExtractFilterCondition(string expression) => "true";
        private string ExtractStaticOptions(string expression) => "[]";
    }
}