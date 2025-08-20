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
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            IFormulaDefinitionRepository formulaDefinitionRepository,
            IFormulaVersionRepository formulaVersionRepository,
            IFormulaEvaluationLogRepository evaluationLogRepository,
            IExpressionEngine expressionEngine,
            ILogger<FormulaService> logger)
        {
            _formulaDefinitionRepository = formulaDefinitionRepository ?? throw new ArgumentNullException(nameof(formulaDefinitionRepository));
            _formulaVersionRepository = formulaVersionRepository ?? throw new ArgumentNullException(nameof(formulaVersionRepository));
            _evaluationLogRepository = evaluationLogRepository ?? throw new ArgumentNullException(nameof(evaluationLogRepository));
            _expressionEngine = expressionEngine ?? throw new ArgumentNullException(nameof(expressionEngine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<Result<FormulaEvaluationResult>> EvaluateFormulaAsync(
            Guid formulaId,
            Dictionary<string, object> inputs,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Evaluating formula {FormulaId} with {InputCount} inputs", formulaId, inputs?.Count ?? 0);

                // Get the latest published version
                var latestVersionResult = await GetLatestPublishedVersionAsync(formulaId, cancellationToken);
                if (!latestVersionResult.IsSuccess)
                {
                    return Result<FormulaEvaluationResult>.Failure(latestVersionResult.Error);
                }

                var formulaVersion = latestVersionResult.Value;
                return await EvaluateFormulaVersionInternalAsync(formulaVersion, inputs, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating formula {FormulaId}", formulaId);
                return Result<FormulaEvaluationResult>.Failure($"Error evaluating formula: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<FormulaEvaluationResult>> EvaluateFormulaByNameAsync(
            string formulaName,
            Guid tenantId,
            Dictionary<string, object> inputs,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Evaluating formula {FormulaName} for tenant {TenantId}", formulaName, tenantId);

                // Get formula definition by name
                var formulaDefinition = await _formulaDefinitionRepository.GetByNameAsync(formulaName, tenantId, cancellationToken);
                if (formulaDefinition == null)
                {
                    return Result<FormulaEvaluationResult>.Failure($"Formula '{formulaName}' not found for tenant {tenantId}");
                }

                return await EvaluateFormulaAsync(formulaDefinition.Id, inputs, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating formula {FormulaName} for tenant {TenantId}", formulaName, tenantId);
                return Result<FormulaEvaluationResult>.Failure($"Error evaluating formula: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<Result<FormulaEvaluationResult>> EvaluateFormulaVersionAsync(
            Guid formulaId,
            int version,
            Dictionary<string, object> inputs,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Evaluating formula {FormulaId} version {Version}", formulaId, version);

                // Get specific version
                var formulaVersion = await _formulaVersionRepository.GetByFormulaAndVersionAsync(formulaId, version, cancellationToken);
                if (formulaVersion == null)
                {
                    return Result<FormulaEvaluationResult>.Failure($"Formula version {version} not found for formula {formulaId}");
                }

                return await EvaluateFormulaVersionInternalAsync(formulaVersion, inputs, cancellationToken);
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

                if (formulaDefinition.Status != FormulaStatus.Published)
                {
                    return Result<FormulaVersion>.Failure($"Formula {formulaId} is not published");
                }

                var latestVersion = await _formulaVersionRepository.GetLatestByFormulaIdAsync(formulaId, cancellationToken);
                if (latestVersion == null)
                {
                    return Result<FormulaVersion>.Failure($"No versions found for formula {formulaId}");
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
                    new FunctionSignature("ADD", "Addition", new[] { "number", "number" }, "number"),
                    new FunctionSignature("SUBTRACT", "Subtraction", new[] { "number", "number" }, "number"),
                    new FunctionSignature("MULTIPLY", "Multiplication", new[] { "number", "number" }, "number"),
                    new FunctionSignature("DIVIDE", "Division", new[] { "number", "number" }, "number"),
                    new FunctionSignature("POWER", "Power", new[] { "number", "number" }, "number"),
                    new FunctionSignature("SQRT", "Square Root", new[] { "number" }, "number"),
                    new FunctionSignature("ABS", "Absolute Value", new[] { "number" }, "number"),
                    new FunctionSignature("ROUND", "Round", new[] { "number", "number" }, "number"),
                    new FunctionSignature("MAX", "Maximum", new[] { "number", "number" }, "number"),
                    new FunctionSignature("MIN", "Minimum", new[] { "number", "number" }, "number"),
                    new FunctionSignature("IF", "Conditional", new[] { "boolean", "any", "any" }, "any"),
                    new FunctionSignature("AND", "Logical AND", new[] { "boolean", "boolean" }, "boolean"),
                    new FunctionSignature("OR", "Logical OR", new[] { "boolean", "boolean" }, "boolean"),
                    new FunctionSignature("NOT", "Logical NOT", new[] { "boolean" }, "boolean")
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
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expression))
                {
                    return Result<bool>.Failure("Expression cannot be empty");
                }

                // Create a formula expression and try to parse it
                var formulaExpression = new FormulaExpression(expression, ExecutionTimeCategory.Runtime);
                
                // Try to evaluate with empty context to check syntax
                var context = new ExpressionEvaluationContext(new Dictionary<string, object>());
                var result = await _expressionEngine.EvaluateAsync(formulaExpression, context, cancellationToken);
                
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
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var evaluationLogId = Guid.NewGuid();
            
            try
            {
                // Create evaluation context
                var context = new ExpressionEvaluationContext(inputs ?? new Dictionary<string, object>());
                
                // Get formula expression
                var formulaExpression = formulaVersion.GetFormulaExpression();
                
                // Evaluate the expression
                var evaluationResult = await _expressionEngine.EvaluateAsync(formulaExpression, context, cancellationToken);
                
                stopwatch.Stop();
                
                // Create evaluation log
                var evaluationLog = FormulaEvaluationLog.Create(
                    evaluationLogId,
                    formulaVersion.FormulaDefinitionId,
                    formulaVersion.VersionNumber,
                    inputs,
                    evaluationResult.Value,
                    evaluationResult.IsSuccess,
                    evaluationResult.ErrorMessage,
                    stopwatch.ElapsedMilliseconds,
                    "system");
                
                // Save evaluation log
                await _evaluationLogRepository.AddAsync(evaluationLog, cancellationToken);
                
                var result = new FormulaEvaluationResult
                {
                    Value = evaluationResult.Value,
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
                var evaluationLog = FormulaEvaluationLog.Create(
                    evaluationLogId,
                    formulaVersion.FormulaDefinitionId,
                    formulaVersion.VersionNumber,
                    inputs,
                    null,
                    false,
                    ex.Message,
                    stopwatch.ElapsedMilliseconds,
                    "system");
                
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
    }
}