using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoreAxis.Modules.DynamicForm.Domain.Interfaces;

/// <summary>
/// Interface for managing field dependencies and incremental recalculation
/// </summary>
public interface IDependencyGraph
{
    /// <summary>
    /// Add a dependency relationship between fields
    /// </summary>
    /// <param name="dependentField">Field that depends on another field</param>
    /// <param name="dependsOnField">Field that the dependent field relies on</param>
    void AddDependency(string dependentField, string dependsOnField);

    /// <summary>
    /// Remove a dependency relationship
    /// </summary>
    /// <param name="dependentField">Field that depends on another field</param>
    /// <param name="dependsOnField">Field that the dependent field relies on</param>
    void RemoveDependency(string dependentField, string dependsOnField);

    /// <summary>
    /// Get all fields that depend on the specified field
    /// </summary>
    /// <param name="fieldName">Name of the field</param>
    /// <returns>List of dependent field names</returns>
    IEnumerable<string> GetDependentFields(string fieldName);

    /// <summary>
    /// Get all fields that the specified field depends on
    /// </summary>
    /// <param name="fieldName">Name of the field</param>
    /// <returns>List of dependency field names</returns>
    IEnumerable<string> GetDependencies(string fieldName);

    /// <summary>
    /// Get fields in topological order for calculation
    /// </summary>
    /// <returns>Fields ordered for safe calculation</returns>
    IEnumerable<string> GetTopologicalOrder();

    /// <summary>
    /// Check if adding a dependency would create a circular reference
    /// </summary>
    /// <param name="dependentField">Field that depends on another field</param>
    /// <param name="dependsOnField">Field that the dependent field relies on</param>
    /// <returns>True if circular dependency would be created</returns>
    bool WouldCreateCircularDependency(string dependentField, string dependsOnField);

    /// <summary>
    /// Get all fields that need to be recalculated when the specified field changes
    /// </summary>
    /// <param name="changedField">Name of the field that changed</param>
    /// <returns>List of fields that need recalculation in correct order</returns>
    IEnumerable<string> GetFieldsToRecalculate(string changedField);

    /// <summary>
    /// Clear all dependencies
    /// </summary>
    void Clear();

    /// <summary>
    /// Get dependency statistics
    /// </summary>
    /// <returns>Dependency graph statistics</returns>
    DependencyGraphStats GetStats();
}

/// <summary>
/// Statistics about the dependency graph
/// </summary>
public class DependencyGraphStats
{
    public int TotalFields { get; set; }
    public int TotalDependencies { get; set; }
    public int MaxDepth { get; set; }
    public IEnumerable<string> RootFields { get; set; } = new List<string>();
    public IEnumerable<string> LeafFields { get; set; } = new List<string>();
    public bool HasCircularDependencies { get; set; }
}

/// <summary>
/// Interface for incremental recalculation engine
/// </summary>
public interface IIncrementalRecalculationEngine
{
    /// <summary>
    /// Recalculate fields that depend on the changed field
    /// </summary>
    /// <param name="formData">Current form data</param>
    /// <param name="changedField">Name of the field that changed</param>
    /// <param name="newValue">New value of the changed field</param>
    /// <param name="dependencyGraph">Dependency graph for the form</param>
    /// <param name="expressionEngine">Expression engine for calculations</param>
    /// <returns>Updated form data with recalculated values</returns>
    Task<Dictionary<string, object?>> RecalculateAsync(
        Dictionary<string, object?> formData,
        string changedField,
        object? newValue,
        IDependencyGraph dependencyGraph,
        IExpressionEngine expressionEngine);

    /// <summary>
    /// Recalculate all calculated fields in the form
    /// </summary>
    /// <param name="formData">Current form data</param>
    /// <param name="dependencyGraph">Dependency graph for the form</param>
    /// <param name="expressionEngine">Expression engine for calculations</param>
    /// <returns>Updated form data with all calculated values</returns>
    Task<Dictionary<string, object?>> RecalculateAllAsync(
        Dictionary<string, object?> formData,
        IDependencyGraph dependencyGraph,
        IExpressionEngine expressionEngine);

    /// <summary>
    /// Get calculation performance metrics
    /// </summary>
    /// <returns>Performance metrics</returns>
    RecalculationMetrics GetMetrics();
}

/// <summary>
/// Performance metrics for recalculation operations
/// </summary>
public class RecalculationMetrics
{
    public int TotalRecalculations { get; set; }
    public TimeSpan TotalCalculationTime { get; set; }
    public TimeSpan AverageCalculationTime { get; set; }
    public int FieldsRecalculated { get; set; }
    public DateTime LastRecalculation { get; set; }
    public Dictionary<string, int> FieldRecalculationCounts { get; set; } = new();
    public Dictionary<string, TimeSpan> FieldCalculationTimes { get; set; } = new();
}