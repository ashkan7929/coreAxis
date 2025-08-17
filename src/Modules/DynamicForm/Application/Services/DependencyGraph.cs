using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Application.Services;

/// <summary>
/// Implementation of dependency graph for managing field dependencies
/// </summary>
public class DependencyGraph : IDependencyGraph
{
    private readonly Dictionary<string, HashSet<string>> _dependencies = new();
    private readonly Dictionary<string, HashSet<string>> _dependents = new();
    private readonly object _lock = new();

    /// <summary>
    /// Add a dependency relationship between fields
    /// </summary>
    public void AddDependency(string dependentField, string dependsOnField)
    {
        if (string.IsNullOrWhiteSpace(dependentField))
            throw new ArgumentException("Dependent field name cannot be null or empty", nameof(dependentField));
        
        if (string.IsNullOrWhiteSpace(dependsOnField))
            throw new ArgumentException("Depends on field name cannot be null or empty", nameof(dependsOnField));

        if (dependentField.Equals(dependsOnField, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A field cannot depend on itself");

        lock (_lock)
        {
            // Check for circular dependency before adding
            if (WouldCreateCircularDependencyInternal(dependentField, dependsOnField))
            {
                throw new InvalidOperationException(
                    $"Adding dependency from '{dependentField}' to '{dependsOnField}' would create a circular dependency");
            }

            // Add to dependencies (dependentField depends on dependsOnField)
            if (!_dependencies.ContainsKey(dependentField))
                _dependencies[dependentField] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _dependencies[dependentField].Add(dependsOnField);

            // Add to dependents (dependsOnField has dependentField as dependent)
            if (!_dependents.ContainsKey(dependsOnField))
                _dependents[dependsOnField] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _dependents[dependsOnField].Add(dependentField);
        }
    }

    /// <summary>
    /// Remove a dependency relationship
    /// </summary>
    public void RemoveDependency(string dependentField, string dependsOnField)
    {
        if (string.IsNullOrWhiteSpace(dependentField) || string.IsNullOrWhiteSpace(dependsOnField))
            return;

        lock (_lock)
        {
            if (_dependencies.ContainsKey(dependentField))
            {
                _dependencies[dependentField].Remove(dependsOnField);
                if (_dependencies[dependentField].Count == 0)
                    _dependencies.Remove(dependentField);
            }

            if (_dependents.ContainsKey(dependsOnField))
            {
                _dependents[dependsOnField].Remove(dependentField);
                if (_dependents[dependsOnField].Count == 0)
                    _dependents.Remove(dependsOnField);
            }
        }
    }

    /// <summary>
    /// Get all fields that depend on the specified field
    /// </summary>
    public IEnumerable<string> GetDependentFields(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return Enumerable.Empty<string>();

        lock (_lock)
        {
            return _dependents.ContainsKey(fieldName) 
                ? _dependents[fieldName].ToList() 
                : Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Get all fields that the specified field depends on
    /// </summary>
    public IEnumerable<string> GetDependencies(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
            return Enumerable.Empty<string>();

        lock (_lock)
        {
            return _dependencies.ContainsKey(fieldName) 
                ? _dependencies[fieldName].ToList() 
                : Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// Get fields in topological order for calculation
    /// </summary>
    public IEnumerable<string> GetTopologicalOrder()
    {
        lock (_lock)
        {
            var result = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Get all fields (both dependencies and dependents)
            var allFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _dependencies)
            {
                allFields.Add(kvp.Key);
                foreach (var dep in kvp.Value)
                    allFields.Add(dep);
            }

            foreach (var field in allFields)
            {
                if (!visited.Contains(field))
                {
                    if (!TopologicalSortVisit(field, visited, visiting, result))
                    {
                        throw new InvalidOperationException("Circular dependency detected in dependency graph");
                    }
                }
            }

            result.Reverse();
            return result;
        }
    }

    /// <summary>
    /// Check if adding a dependency would create a circular reference
    /// </summary>
    public bool WouldCreateCircularDependency(string dependentField, string dependsOnField)
    {
        if (string.IsNullOrWhiteSpace(dependentField) || string.IsNullOrWhiteSpace(dependsOnField))
            return false;

        lock (_lock)
        {
            return WouldCreateCircularDependencyInternal(dependentField, dependsOnField);
        }
    }

    /// <summary>
    /// Get all fields that need to be recalculated when the specified field changes
    /// </summary>
    public IEnumerable<string> GetFieldsToRecalculate(string changedField)
    {
        if (string.IsNullOrWhiteSpace(changedField))
            return Enumerable.Empty<string>();

        lock (_lock)
        {
            var fieldsToRecalculate = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            CollectDependentFields(changedField, fieldsToRecalculate, visited);

            // Sort by dependency order to ensure correct calculation sequence
            var topologicalOrder = GetTopologicalOrder().ToList();
            return fieldsToRecalculate
                .OrderBy(field => topologicalOrder.IndexOf(field))
                .ToList();
        }
    }

    /// <summary>
    /// Clear all dependencies
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _dependencies.Clear();
            _dependents.Clear();
        }
    }

    /// <summary>
    /// Get dependency statistics
    /// </summary>
    public DependencyGraphStats GetStats()
    {
        lock (_lock)
        {
            var allFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in _dependencies)
            {
                allFields.Add(kvp.Key);
                foreach (var dep in kvp.Value)
                    allFields.Add(dep);
            }

            var rootFields = allFields.Where(field => !_dependencies.ContainsKey(field) || _dependencies[field].Count == 0).ToList();
            var leafFields = allFields.Where(field => !_dependents.ContainsKey(field) || _dependents[field].Count == 0).ToList();

            var maxDepth = CalculateMaxDepth();
            var hasCircularDependencies = HasCircularDependencies();

            return new DependencyGraphStats
            {
                TotalFields = allFields.Count,
                TotalDependencies = _dependencies.Values.Sum(deps => deps.Count),
                MaxDepth = maxDepth,
                RootFields = rootFields,
                LeafFields = leafFields,
                HasCircularDependencies = hasCircularDependencies
            };
        }
    }

    #region Private Methods

    private bool WouldCreateCircularDependencyInternal(string dependentField, string dependsOnField)
    {
        // If dependsOnField eventually depends on dependentField, adding this dependency would create a cycle
        return HasPath(dependsOnField, dependentField);
    }

    private bool HasPath(string from, string to)
    {
        if (from.Equals(to, StringComparison.OrdinalIgnoreCase))
            return true;

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return HasPathRecursive(from, to, visited);
    }

    private bool HasPathRecursive(string current, string target, HashSet<string> visited)
    {
        if (current.Equals(target, StringComparison.OrdinalIgnoreCase))
            return true;

        if (visited.Contains(current))
            return false;

        visited.Add(current);

        if (_dependencies.ContainsKey(current))
        {
            foreach (var dependency in _dependencies[current])
            {
                if (HasPathRecursive(dependency, target, visited))
                    return true;
            }
        }

        return false;
    }

    private bool TopologicalSortVisit(string field, HashSet<string> visited, HashSet<string> visiting, List<string> result)
    {
        if (visiting.Contains(field))
            return false; // Circular dependency detected

        if (visited.Contains(field))
            return true;

        visiting.Add(field);

        if (_dependencies.ContainsKey(field))
        {
            foreach (var dependency in _dependencies[field])
            {
                if (!TopologicalSortVisit(dependency, visited, visiting, result))
                    return false;
            }
        }

        visiting.Remove(field);
        visited.Add(field);
        result.Add(field);

        return true;
    }

    private void CollectDependentFields(string field, List<string> result, HashSet<string> visited)
    {
        if (visited.Contains(field))
            return;

        visited.Add(field);

        if (_dependents.ContainsKey(field))
        {
            foreach (var dependent in _dependents[field])
            {
                result.Add(dependent);
                CollectDependentFields(dependent, result, visited);
            }
        }
    }

    private int CalculateMaxDepth()
    {
        var maxDepth = 0;
        var allFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var kvp in _dependencies)
        {
            allFields.Add(kvp.Key);
            foreach (var dep in kvp.Value)
                allFields.Add(dep);
        }

        foreach (var field in allFields)
        {
            var depth = CalculateDepth(field, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            maxDepth = Math.Max(maxDepth, depth);
        }

        return maxDepth;
    }

    private int CalculateDepth(string field, HashSet<string> visited)
    {
        if (visited.Contains(field))
            return 0; // Avoid infinite recursion

        visited.Add(field);

        if (!_dependencies.ContainsKey(field) || _dependencies[field].Count == 0)
            return 0;

        var maxDepth = 0;
        foreach (var dependency in _dependencies[field])
        {
            var depth = CalculateDepth(dependency, new HashSet<string>(visited, StringComparer.OrdinalIgnoreCase));
            maxDepth = Math.Max(maxDepth, depth + 1);
        }

        return maxDepth;
    }

    private bool HasCircularDependencies()
    {
        try
        {
            GetTopologicalOrder();
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    #endregion
}