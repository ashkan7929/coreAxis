using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Entities;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using CoreAxis.Modules.DynamicForm.Domain.Repositories;
using CoreAxis.Modules.DynamicForm.Domain.ValueObjects;

using CoreAxis.SharedKernel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services
{
    /// <summary>
    /// Implementation of formula service that provides formula evaluation and management capabilities.
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly IFormulaDefinitionRepository _formulaDefinitionRepository;
        private readonly IFormulaVersionRepository _formulaVersionRepository;
        private readonly IFormulaEvaluationLogRepository _evaluationLogRepository;
        private readonly IExpressionEngine _expressionEngine;
        private readonly IRoundingPolicy _roundingPolicy;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            IFormulaDefinitionRepository formulaDefinitionRepository,
            IFormulaVersionRepository formulaVersionRepository,
            IFormulaEvaluationLogRepository evaluationLogRepository,
            IExpressionEngine expressionEngine,
            IRoundingPolicy roundingPolicy,
            ILogger<FormulaService> logger)
        {
            _formulaDefinitionRepository = formulaDefinitionRepository ?? throw new ArgumentNullException(nameof(formulaDefinitionRepository));
            _formulaVersionRepository = formulaVersionRepository ?? throw new ArgumentNullException(nameof(formulaVersionRepository));
            _evaluationLogRepository = evaluationLogRepository ?? throw new ArgumentNullException(nameof(evaluationLogRepository));
            _expressionEngine = expressionEngine ?? throw new ArgumentNullException(nameof(expressionEngine));
            _roundingPolicy = roundingPolicy ?? throw new ArgumentNullException(nameof(roundingPolicy));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Result<FormulaEvaluationResult>> EvaluateFormulaAsync(
            Guid formulaId,
            Dictionary<string, object> inputs,
            Dictionary<string, object>? context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Evaluating formula {FormulaId} with {InputCount} inputs", formulaId, inputs?.Count ?? 0);

                // Get the latest published version
                var latestVersionResult = await GetLatestPublishedVersionAsync(formulaId, cancellationToken);
                if (!latestVersionResult.IsSuccess)
                {
                    return Result<FormulaEvaluationResult>.Failure(string.Join("; ", latestVersionResult.Errors));
                }

                var formulaVersion = latestVersionResult.Value;
                return await EvaluateFormulaVersionInternalAsync(formulaVersion, inputs, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating formula {FormulaId}", formulaId);
                return Result<FormulaEvaluationResult>.Failure($"Error evaluating formula: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<FormulaEvaluationResult>> EvaluateFormulaAsync(
            string formulaName,
            int? version,
            Dictionary<string, object> inputs,
            Dictionary<string, object>? context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Evaluating formula {FormulaName} (version: {Version})", formulaName, version?.ToString() ?? "latest");

                // Get formula definition by name
                var formulaDefinition = await _formulaDefinitionRepository.GetByNameAsync(formulaName, cancellationToken);
                if (formulaDefinition == null)
                {
                    return Result<FormulaEvaluationResult>.Failure($"Formula '{formulaName}' not found");
                }

                if (version.HasValue)
                {
                    // Evaluate a specific pinned version; must be published, active, and within effective window
                    var pinnedVersion = await _formulaVersionRepository.GetByFormulaDefinitionIdAndVersionAsync(formulaDefinition.Id, version.Value, cancellationToken);
                    if (pinnedVersion == null)
                    {
                        return Result<FormulaEvaluationResult>.Failure($"Formula version {version.Value} not found for '{formulaName}'");
                    }

                    if (!pinnedVersion.IsPublished)
                    {
                        return Result<FormulaEvaluationResult>.Failure($"Pinned version {version.Value} is not published");
                    }

                    var now = DateTime.UtcNow;
                    var effective = (pinnedVersion.EffectiveFrom == null || pinnedVersion.EffectiveFrom <= now)
                                 && (pinnedVersion.EffectiveTo == null || pinnedVersion.EffectiveTo >= now);

                    if (!effective)
                    {
                        return Result<FormulaEvaluationResult>.Failure($"Pinned version {version.Value} is outside effective window");
                    }

                    if (!pinnedVersion.IsActive)
                    {
                        return Result<FormulaEvaluationResult>.Failure($"Pinned version {version.Value} is not active");
                    }

                    return await EvaluateFormulaVersionInternalAsync(pinnedVersion, inputs, context, cancellationToken);
                }

                // No version pinned: resolve latest effective published & active
                var latestVersionResult = await GetLatestPublishedVersionAsync(formulaDefinition.Id, cancellationToken);
                if (!latestVersionResult.IsSuccess)
                {
                    return Result<FormulaEvaluationResult>.Failure(string.Join("; ", latestVersionResult.Errors));
                }

                return await EvaluateFormulaVersionInternalAsync(latestVersionResult.Value, inputs, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating formula {FormulaName}", formulaName);
                return Result<FormulaEvaluationResult>.Failure($"Error evaluating formula: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<FormulaEvaluationResult>> EvaluateFormulaVersionAsync(
            Guid formulaId,
            int version,
            Dictionary<string, object> inputs,
            Dictionary<string, object>? context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Evaluating formula {FormulaId} version {Version}", formulaId, version);

                // Get specific version
                var formulaVersion = await _formulaVersionRepository.GetByFormulaDefinitionIdAndVersionAsync(formulaId, version, cancellationToken);
                if (formulaVersion == null)
                {
                    return Result<FormulaEvaluationResult>.Failure($"Formula version {version} not found for formula {formulaId}");
                }

                // Enforce published and effective window
                if (!formulaVersion.IsPublished)
                {
                    return Result<FormulaEvaluationResult>.Failure($"Formula version {version} is not published");
                }

                var now = DateTime.UtcNow;
                var effective = (formulaVersion.EffectiveFrom == null || formulaVersion.EffectiveFrom <= now)
                             && (formulaVersion.EffectiveTo == null || formulaVersion.EffectiveTo >= now);

                if (!effective)
                {
                    return Result<FormulaEvaluationResult>.Failure($"Formula version {version} is outside effective window");
                }

                if (!formulaVersion.IsActive)
                {
                    return Result<FormulaEvaluationResult>.Failure($"Formula version {version} is not active");
                }

                return await EvaluateFormulaVersionInternalAsync(formulaVersion, inputs, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating formula {FormulaId} version {Version}", formulaId, version);
                return Result<FormulaEvaluationResult>.Failure($"Error evaluating formula version: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<FormulaVersion>> GetLatestPublishedVersionAsync(
            Guid formulaId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var formulaDefinition = await _formulaDefinitionRepository.GetByIdAsync(formulaId, cancellationToken);
                if (formulaDefinition == null)
                {
                    return Result<FormulaVersion>.Failure($"Formula {formulaId} not found");
                }

                if (!formulaDefinition.IsPublished)
                {
                    return Result<FormulaVersion>.Failure($"Formula {formulaId} is not published");
                }

                var latestVersion = await _formulaVersionRepository.GetLatestPublishedEffectiveVersionAsync(formulaId, DateTime.UtcNow, cancellationToken);
                if (latestVersion == null)
                {
                    return Result<FormulaVersion>.Failure($"No effective published version found for formula {formulaId}");
                }

                return Result<FormulaVersion>.Success(latestVersion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest published version for formula {FormulaId}", formulaId);
                return Result<FormulaVersion>.Failure($"Error getting latest version: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<IEnumerable<FunctionSignature>>> GetAvailableFunctionsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                // This would typically come from the expression engine
                // For now, return a basic set of mathematical functions
                var functions = new List<FunctionSignature>
                {
                    new FunctionSignature("ADD", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                    new FunctionSignature("SUBTRACT", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                    new FunctionSignature("MULTIPLY", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                    new FunctionSignature("DIVIDE", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                    new FunctionSignature("POWER", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                    new FunctionSignature("SQRT", typeof(decimal), new[] { typeof(decimal) }),
                    new FunctionSignature("ABS", typeof(decimal), new[] { typeof(decimal) }),
                    new FunctionSignature("ROUND", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                    new FunctionSignature("MAX", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                    new FunctionSignature("MIN", typeof(decimal), new[] { typeof(decimal), typeof(decimal) }),
                    new FunctionSignature("IF", typeof(object), new[] { typeof(bool), typeof(object), typeof(object) }),
                    new FunctionSignature("AND", typeof(bool), new[] { typeof(bool), typeof(bool) }),
                    new FunctionSignature("OR", typeof(bool), new[] { typeof(bool), typeof(bool) }),
                    new FunctionSignature("NOT", typeof(bool), new[] { typeof(bool) })
                };

                return Result<IEnumerable<FunctionSignature>>.Success(functions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available functions");
                return Result<IEnumerable<FunctionSignature>>.Failure($"Error getting available functions: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<bool>> ValidateExpressionAsync(
            string expression,
            Dictionary<string, object>? context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expression))
                {
                    return Result<bool>.Failure("Expression cannot be empty");
                }

                // Create a formula expression and try to parse it
                var formulaExpression = new FormulaExpression(expression);
                
                // Try to evaluate with empty context to check syntax
                var evalContext = BuildEvaluationContext(new Dictionary<string, object>(), context);
                var result = await _expressionEngine.EvaluateAsync(formulaExpression, evalContext, cancellationToken);
                
                return Result<bool>.Success(result.IsSuccess);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Expression validation failed for: {Expression}", expression);
                return Result<bool>.Failure($"Invalid expression: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<PaginatedList<FormulaEvaluationLog>>> GetEvaluationHistoryAsync(
            Guid formulaId,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var logs = await _evaluationLogRepository.GetByFormulaDefinitionIdAsync(formulaId, cancellationToken);
                var totalCount = logs.Count();
                
                var pagedLogs = logs
                    .OrderByDescending(l => l.CreatedOn)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResult = PaginatedList<FormulaEvaluationLog>.Create(
                    pagedLogs,
                    pageNumber,
                    pageSize,
                    totalCount);

                return Result<PaginatedList<FormulaEvaluationLog>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting evaluation history for formula {FormulaId}", formulaId);
                return Result<PaginatedList<FormulaEvaluationLog>>.Failure($"Error getting evaluation history: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<FormulaPerformanceMetrics>> GetPerformanceMetricsAsync(
            Guid formulaId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var logs = await _evaluationLogRepository.GetByFormulaDefinitionIdAsync(formulaId, cancellationToken);
                var evaluationLogs = logs.ToList();

                if (!evaluationLogs.Any())
                {
                    return Result<FormulaPerformanceMetrics>.Success(new FormulaPerformanceMetrics());
                }

                var successfulEvaluations = evaluationLogs.Where(l => l.IsSuccess).ToList();
                var failedEvaluations = evaluationLogs.Where(l => !l.IsSuccess).ToList();

                var metrics = new FormulaPerformanceMetrics
                {
                    TotalEvaluations = evaluationLogs.Count,
                    FailedEvaluations = failedEvaluations.Count,
                    SuccessRate = evaluationLogs.Count > 0 ? (double)successfulEvaluations.Count / evaluationLogs.Count * 100 : 0,
                    LastEvaluationAt = evaluationLogs.Max(l => l.CreatedOn)
                };

                if (successfulEvaluations.Any())
                {
                    var executionTimes = successfulEvaluations.Select(l => l.ExecutionTimeMs).ToList();
                    metrics.AverageEvaluationTimeMs = executionTimes.Average();
                    metrics.MinEvaluationTimeMs = executionTimes.Min();
                    metrics.MaxEvaluationTimeMs = executionTimes.Max();
                }

                return Result<FormulaPerformanceMetrics>.Success(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance metrics for formula {FormulaId}", formulaId);
                return Result<FormulaPerformanceMetrics>.Failure($"Error getting performance metrics: {ex.Message}");
            }
        }

        private async Task<Result<FormulaEvaluationResult>> EvaluateFormulaVersionInternalAsync(
            FormulaVersion formulaVersion,
            Dictionary<string, object> inputs,
            Dictionary<string, object>? context,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var evaluationLogId = Guid.NewGuid();
            
            try
            {
                // Create evaluation context
                var evalContext = BuildEvaluationContext(inputs, context);
                
                // Get formula expression
                var formulaExpression = formulaVersion.GetFormulaExpression();
                
                // Evaluate the expression
                var evaluationResult = await _expressionEngine.EvaluateAsync(formulaExpression, evalContext, cancellationToken);

                // Normalize monetary decimals using rounding policy
                object? normalizedValue = evaluationResult.Value;
                if (normalizedValue is decimal dv)
                {
                    normalizedValue = _roundingPolicy.NormalizeMoney(dv);
                }
                
                stopwatch.Stop();
                
                // Create evaluation log
                var evaluationLog = new FormulaEvaluationLog
                {
                    Id = evaluationLogId,
                    FormulaDefinitionId = formulaVersion.FormulaDefinitionId,
                    FormulaVersionId = formulaVersion.Id,
                    InputParameters = JsonSerializer.Serialize(inputs),
                    Result = JsonSerializer.Serialize(normalizedValue),
                    Status = evaluationResult.IsSuccess ? "Success" : "Failed",
                    ErrorMessage = evaluationResult.ErrorMessage,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    CreatedBy = "system",
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true
                };
                
                // Save evaluation log
                await _evaluationLogRepository.AddAsync(evaluationLog, cancellationToken);
                
                var result = new FormulaEvaluationResult
                {
                    Value = normalizedValue!,
                    FormulaVersion = formulaVersion.VersionNumber,
                    EvaluationDurationMs = stopwatch.ElapsedMilliseconds,
                    EvaluationLogId = evaluationLogId,
                    IsSuccess = evaluationResult.IsSuccess,
                    ErrorMessage = evaluationResult.ErrorMessage,
                    Metadata = new Dictionary<string, object>
                    {
                        { "formulaId", formulaVersion.FormulaDefinitionId },
                        { "version", formulaVersion.VersionNumber },
                        { "evaluatedAt", DateTime.UtcNow },
                        { "inputCount", inputs?.Count ?? 0 }
                    }
                };
                
                return Result<FormulaEvaluationResult>.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _logger.LogError(ex, "Error evaluating formula version {FormulaId}:{Version}", 
                    formulaVersion.FormulaDefinitionId, formulaVersion.VersionNumber);
                
                // Create failed evaluation log
                var evaluationLog = new FormulaEvaluationLog
                {
                    Id = evaluationLogId,
                    FormulaDefinitionId = formulaVersion.FormulaDefinitionId,
                    FormulaVersionId = formulaVersion.Id,
                    InputParameters = JsonSerializer.Serialize(inputs),
                    Result = null,
                    Status = "Failed",
                    ErrorMessage = ex.Message,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    CreatedBy = "system",
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true
                };
                
                try
                {
                    await _evaluationLogRepository.AddAsync(evaluationLog, cancellationToken);
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Error saving evaluation log");
                }
                
                return Result<FormulaEvaluationResult>.Failure($"Error evaluating formula: {ex.Message}");
            }
        }

        private ExpressionEvaluationContext BuildEvaluationContext(
            Dictionary<string, object> inputs,
            Dictionary<string, object>? context)
        {
            var evalContext = new ExpressionEvaluationContext();

            // Populate variables with inputs
            if (inputs != null)
            {
                foreach (var kvp in inputs)
                {
                    evalContext.AddVariable(kvp.Key, kvp.Value);
                }
                evalContext.FormData = inputs;
            }

            // Store provided context (if any)
            if (context != null)
            {
                evalContext.UserContext = context;
            }

            return evalContext;
        }
    }
}