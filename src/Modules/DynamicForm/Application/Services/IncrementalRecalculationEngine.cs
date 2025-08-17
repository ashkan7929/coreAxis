using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Application.Services;

/// <summary>
/// Implementation of incremental recalculation engine for form fields
/// </summary>
public class IncrementalRecalculationEngine : IIncrementalRecalculationEngine
{
    private readonly ILogger<IncrementalRecalculationEngine> _logger;
    private readonly RecalculationMetrics _metrics;
    private readonly object _metricsLock = new();

    public IncrementalRecalculationEngine(ILogger<IncrementalRecalculationEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = new RecalculationMetrics();
    }

    /// <summary>
    /// Recalculate fields that depend on the changed field
    /// </summary>
    public async Task<Dictionary<string, object?>> RecalculateAsync(
        Dictionary<string, object?> formData,
        string changedField,
        object? newValue,
        IDependencyGraph dependencyGraph,
        IExpressionEngine expressionEngine)
    {
        if (formData == null)
            throw new ArgumentNullException(nameof(formData));
        
        if (string.IsNullOrWhiteSpace(changedField))
            throw new ArgumentException("Changed field name cannot be null or empty", nameof(changedField));
        
        if (dependencyGraph == null)
            throw new ArgumentNullException(nameof(dependencyGraph));
        
        if (expressionEngine == null)
            throw new ArgumentNullException(nameof(expressionEngine));

        var stopwatch = Stopwatch.StartNew();
        var updatedFormData = new Dictionary<string, object?>(formData, StringComparer.OrdinalIgnoreCase);
        
        try
        {
            _logger.LogDebug("Starting incremental recalculation for field '{ChangedField}' with value '{NewValue}'", 
                changedField, newValue);

            // Update the changed field value
            updatedFormData[changedField] = newValue;

            // Get fields that need to be recalculated
            var fieldsToRecalculate = dependencyGraph.GetFieldsToRecalculate(changedField).ToList();
            
            if (fieldsToRecalculate.Count == 0)
            {
                _logger.LogDebug("No dependent fields found for '{ChangedField}'", changedField);
                return updatedFormData;
            }

            _logger.LogDebug("Found {Count} fields to recalculate: {Fields}", 
                fieldsToRecalculate.Count, string.Join(", ", fieldsToRecalculate));

            // Recalculate each dependent field in dependency order
            var recalculatedCount = 0;
            foreach (var fieldToRecalculate in fieldsToRecalculate)
            {
                var fieldStopwatch = Stopwatch.StartNew();
                
                try
                {
                    // Get the field's calculation expression (this would come from form schema)
                    // For now, we'll assume the expression is stored in a special format
                    var fieldExpression = GetFieldExpression(fieldToRecalculate, updatedFormData);
                    
                    if (!string.IsNullOrWhiteSpace(fieldExpression))
                    {
                        var context = new ExpressionEvaluationContext
                        {
                            Variables = updatedFormData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase),
                            Functions = new Dictionary<string, object>(),
                            Timeout = TimeSpan.FromSeconds(5)
                        };

                        var result = await expressionEngine.EvaluateAsync(fieldExpression, context);
                        
                        if (result.IsSuccess)
                        {
                            updatedFormData[fieldToRecalculate] = result.Value;
                            recalculatedCount++;
                            
                            _logger.LogDebug("Successfully recalculated field '{Field}' with value '{Value}'", 
                                fieldToRecalculate, result.Value);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to recalculate field '{Field}': {Error}", 
                                fieldToRecalculate, result.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error recalculating field '{Field}'", fieldToRecalculate);
                }
                finally
                {
                    fieldStopwatch.Stop();
                    UpdateFieldMetrics(fieldToRecalculate, fieldStopwatch.Elapsed);
                }
            }

            _logger.LogInformation("Incremental recalculation completed. Recalculated {RecalculatedCount} out of {TotalCount} fields", 
                recalculatedCount, fieldsToRecalculate.Count);

            return updatedFormData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during incremental recalculation for field '{ChangedField}'", changedField);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            UpdateMetrics(stopwatch.Elapsed, fieldsToRecalculate?.Count ?? 0);
        }
    }

    /// <summary>
    /// Recalculate all calculated fields in the form
    /// </summary>
    public async Task<Dictionary<string, object?>> RecalculateAllAsync(
        Dictionary<string, object?> formData,
        IDependencyGraph dependencyGraph,
        IExpressionEngine expressionEngine)
    {
        if (formData == null)
            throw new ArgumentNullException(nameof(formData));
        
        if (dependencyGraph == null)
            throw new ArgumentNullException(nameof(dependencyGraph));
        
        if (expressionEngine == null)
            throw new ArgumentNullException(nameof(expressionEngine));

        var stopwatch = Stopwatch.StartNew();
        var updatedFormData = new Dictionary<string, object?>(formData, StringComparer.OrdinalIgnoreCase);
        
        try
        {
            _logger.LogDebug("Starting full recalculation of all calculated fields");

            // Get all fields in topological order
            var fieldsInOrder = dependencyGraph.GetTopologicalOrder().ToList();
            
            if (fieldsInOrder.Count == 0)
            {
                _logger.LogDebug("No fields found in dependency graph");
                return updatedFormData;
            }

            _logger.LogDebug("Recalculating {Count} fields in topological order: {Fields}", 
                fieldsInOrder.Count, string.Join(", ", fieldsInOrder));

            var recalculatedCount = 0;
            foreach (var field in fieldsInOrder)
            {
                var fieldStopwatch = Stopwatch.StartNew();
                
                try
                {
                    // Get the field's calculation expression
                    var fieldExpression = GetFieldExpression(field, updatedFormData);
                    
                    if (!string.IsNullOrWhiteSpace(fieldExpression))
                    {
                        var context = new ExpressionEvaluationContext
                        {
                            Variables = updatedFormData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase),
                            Functions = new Dictionary<string, object>(),
                            Timeout = TimeSpan.FromSeconds(5)
                        };

                        var result = await expressionEngine.EvaluateAsync(fieldExpression, context);
                        
                        if (result.IsSuccess)
                        {
                            updatedFormData[field] = result.Value;
                            recalculatedCount++;
                            
                            _logger.LogDebug("Successfully recalculated field '{Field}' with value '{Value}'", 
                                field, result.Value);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to recalculate field '{Field}': {Error}", 
                                field, result.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error recalculating field '{Field}'", field);
                }
                finally
                {
                    fieldStopwatch.Stop();
                    UpdateFieldMetrics(field, fieldStopwatch.Elapsed);
                }
            }

            _logger.LogInformation("Full recalculation completed. Recalculated {RecalculatedCount} out of {TotalCount} fields", 
                recalculatedCount, fieldsInOrder.Count);

            return updatedFormData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full recalculation");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            UpdateMetrics(stopwatch.Elapsed, fieldsInOrder?.Count ?? 0);
        }
    }

    /// <summary>
    /// Get calculation performance metrics
    /// </summary>
    public RecalculationMetrics GetMetrics()
    {
        lock (_metricsLock)
        {
            return new RecalculationMetrics
            {
                TotalRecalculations = _metrics.TotalRecalculations,
                TotalCalculationTime = _metrics.TotalCalculationTime,
                AverageCalculationTime = _metrics.TotalRecalculations > 0 
                    ? TimeSpan.FromTicks(_metrics.TotalCalculationTime.Ticks / _metrics.TotalRecalculations)
                    : TimeSpan.Zero,
                FieldsRecalculated = _metrics.FieldsRecalculated,
                LastRecalculation = _metrics.LastRecalculation,
                FieldRecalculationCounts = new Dictionary<string, int>(_metrics.FieldRecalculationCounts),
                FieldCalculationTimes = new Dictionary<string, TimeSpan>(_metrics.FieldCalculationTimes)
            };
        }
    }

    #region Private Methods

    private string? GetFieldExpression(string fieldName, Dictionary<string, object?> formData)
    {
        // In a real implementation, this would retrieve the calculation expression from the form schema
        // For now, we'll look for a special naming convention or metadata
        
        // Check if there's a calculation expression stored with a special key
        var expressionKey = $"_calc_{fieldName}";
        if (formData.ContainsKey(expressionKey) && formData[expressionKey] is string expression)
        {
            return expression;
        }

        // Check if there's a metadata object that contains the expression
        var metadataKey = $"_meta_{fieldName}";
        if (formData.ContainsKey(metadataKey) && formData[metadataKey] is Dictionary<string, object> metadata)
        {
            if (metadata.ContainsKey("calculation") && metadata["calculation"] is string calcExpression)
            {
                return calcExpression;
            }
        }

        // For demo purposes, return some sample expressions based on field names
        return fieldName.ToLowerInvariant() switch
        {
            "total" => "price * quantity",
            "tax" => "total * 0.1",
            "grandtotal" => "total + tax",
            "discount" => "total * discountrate",
            "finalamount" => "grandtotal - discount",
            _ => null
        };
    }

    private void UpdateMetrics(TimeSpan calculationTime, int fieldsRecalculated)
    {
        lock (_metricsLock)
        {
            _metrics.TotalRecalculations++;
            _metrics.TotalCalculationTime = _metrics.TotalCalculationTime.Add(calculationTime);
            _metrics.FieldsRecalculated += fieldsRecalculated;
            _metrics.LastRecalculation = DateTime.UtcNow;
        }
    }

    private void UpdateFieldMetrics(string fieldName, TimeSpan calculationTime)
    {
        lock (_metricsLock)
        {
            if (!_metrics.FieldRecalculationCounts.ContainsKey(fieldName))
            {
                _metrics.FieldRecalculationCounts[fieldName] = 0;
                _metrics.FieldCalculationTimes[fieldName] = TimeSpan.Zero;
            }

            _metrics.FieldRecalculationCounts[fieldName]++;
            _metrics.FieldCalculationTimes[fieldName] = _metrics.FieldCalculationTimes[fieldName].Add(calculationTime);
        }
    }

    #endregion
}