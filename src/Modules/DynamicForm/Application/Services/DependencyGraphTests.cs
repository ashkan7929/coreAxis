using CoreAxis.Modules.DynamicForm.Application.Services;
using CoreAxis.Modules.DynamicForm.Domain.Interfaces;
using Xunit;
using System;
using System.Linq;

namespace CoreAxis.Modules.DynamicForm.Application.Services;

/// <summary>
/// Unit tests for DependencyGraph
/// </summary>
public class DependencyGraphTests
{
    private IDependencyGraph CreateDependencyGraph()
    {
        return new DependencyGraph();
    }

    [Fact]
    public void AddDependency_ValidDependency_ShouldAddSuccessfully()
    {
        // Arrange
        var graph = CreateDependencyGraph();

        // Act
        graph.AddDependency("total", "price");
        graph.AddDependency("total", "quantity");

        // Assert
        var dependencies = graph.GetDependencies("total").ToList();
        Assert.Contains("price", dependencies);
        Assert.Contains("quantity", dependencies);
        Assert.Equal(2, dependencies.Count);

        var dependents = graph.GetDependentFields("price").ToList();
        Assert.Contains("total", dependents);
    }

    [Fact]
    public void AddDependency_SelfDependency_ShouldThrowException()
    {
        // Arrange
        var graph = CreateDependencyGraph();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => graph.AddDependency("field1", "field1"));
    }

    [Fact]
    public void AddDependency_NullOrEmptyFields_ShouldThrowException()
    {
        // Arrange
        var graph = CreateDependencyGraph();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => graph.AddDependency("", "field1"));
        Assert.Throws<ArgumentException>(() => graph.AddDependency("field1", ""));
        Assert.Throws<ArgumentException>(() => graph.AddDependency(null, "field1"));
        Assert.Throws<ArgumentException>(() => graph.AddDependency("field1", null));
    }

    [Fact]
    public void AddDependency_CircularDependency_ShouldThrowException()
    {
        // Arrange
        var graph = CreateDependencyGraph();
        graph.AddDependency("A", "B");
        graph.AddDependency("B", "C");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => graph.AddDependency("C", "A"));
    }

    [Fact]
    public void RemoveDependency_ExistingDependency_ShouldRemoveSuccessfully()
    {
        // Arrange
        var graph = CreateDependencyGraph();
        graph.AddDependency("total", "price");
        graph.AddDependency("total", "quantity");

        // Act
        graph.RemoveDependency("total", "price");

        // Assert
        var dependencies = graph.GetDependencies("total").ToList();
        Assert.DoesNotContain("price", dependencies);
        Assert.Contains("quantity", dependencies);
        Assert.Single(dependencies);

        var dependents = graph.GetDependentFields("price").ToList();
        Assert.DoesNotContain("total", dependents);
    }

    [Fact]
    public void GetTopologicalOrder_ValidGraph_ShouldReturnCorrectOrder()
    {
        // Arrange
        var graph = CreateDependencyGraph();
        graph.AddDependency("total", "price");
        graph.AddDependency("total", "quantity");
        graph.AddDependency("tax", "total");
        graph.AddDependency("grandTotal", "total");
        graph.AddDependency("grandTotal", "tax");

        // Act
        var order = graph.GetTopologicalOrder().ToList();

        // Assert
        var priceIndex = order.IndexOf("price");
        var quantityIndex = order.IndexOf("quantity");
        var totalIndex = order.IndexOf("total");
        var taxIndex = order.IndexOf("tax");
        var grandTotalIndex = order.IndexOf("grandTotal");

        Assert.True(priceIndex < totalIndex);
        Assert.True(quantityIndex < totalIndex);
        Assert.True(totalIndex < taxIndex);
        Assert.True(totalIndex < grandTotalIndex);
        Assert.True(taxIndex < grandTotalIndex);
    }

    [Fact]
    public void WouldCreateCircularDependency_ValidCheck_ShouldReturnCorrectResult()
    {
        // Arrange
        var graph = CreateDependencyGraph();
        graph.AddDependency("A", "B");
        graph.AddDependency("B", "C");

        // Act & Assert
        Assert.True(graph.WouldCreateCircularDependency("C", "A"));
        Assert.True(graph.WouldCreateCircularDependency("C", "B"));
        Assert.False(graph.WouldCreateCircularDependency("D", "A"));
        Assert.False(graph.WouldCreateCircularDependency("A", "D"));
    }

    [Fact]
    public void GetFieldsToRecalculate_ValidField_ShouldReturnDependentsInOrder()
    {
        // Arrange
        var graph = CreateDependencyGraph();
        graph.AddDependency("total", "price");
        graph.AddDependency("total", "quantity");
        graph.AddDependency("tax", "total");
        graph.AddDependency("grandTotal", "total");
        graph.AddDependency("grandTotal", "tax");

        // Act
        var fieldsToRecalculate = graph.GetFieldsToRecalculate("price").ToList();

        // Assert
        Assert.Contains("total", fieldsToRecalculate);
        Assert.Contains("tax", fieldsToRecalculate);
        Assert.Contains("grandTotal", fieldsToRecalculate);
        
        var totalIndex = fieldsToRecalculate.IndexOf("total");
        var taxIndex = fieldsToRecalculate.IndexOf("tax");
        var grandTotalIndex = fieldsToRecalculate.IndexOf("grandTotal");
        
        Assert.True(totalIndex < taxIndex);
        Assert.True(totalIndex < grandTotalIndex);
        Assert.True(taxIndex < grandTotalIndex);
    }

    [Fact]
    public void GetStats_ValidGraph_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var graph = CreateDependencyGraph();
        graph.AddDependency("total", "price");
        graph.AddDependency("total", "quantity");
        graph.AddDependency("tax", "total");

        // Act
        var stats = graph.GetStats();

        // Assert
        Assert.Equal(4, stats.TotalFields); // price, quantity, total, tax
        Assert.Equal(3, stats.TotalDependencies); // total->price, total->quantity, tax->total
        Assert.False(stats.HasCircularDependencies);
        Assert.Contains("price", stats.RootFields);
        Assert.Contains("quantity", stats.RootFields);
        Assert.Contains("tax", stats.LeafFields);
    }

    [Fact]
    public void Clear_WithDependencies_ShouldRemoveAllDependencies()
    {
        // Arrange
        var graph = CreateDependencyGraph();
        graph.AddDependency("total", "price");
        graph.AddDependency("total", "quantity");
        graph.AddDependency("tax", "total");

        // Act
        graph.Clear();

        // Assert
        var stats = graph.GetStats();
        Assert.Equal(0, stats.TotalFields);
        Assert.Equal(0, stats.TotalDependencies);
        Assert.Empty(graph.GetDependencies("total"));
        Assert.Empty(graph.GetDependentFields("price"));
    }

    [Fact]
    public void GetDependentFields_NonExistentField_ShouldReturnEmpty()
    {
        // Arrange
        var graph = CreateDependencyGraph();
        graph.AddDependency("total", "price");

        // Act
        var dependents = graph.GetDependentFields("nonexistent").ToList();

        // Assert
        Assert.Empty(dependents);
    }

    [Fact]
    public void GetDependencies_NonExistentField_ShouldReturnEmpty()
    {
        // Arrange
        var graph = CreateDependencyGraph();
        graph.AddDependency("total", "price");

        // Act
        var dependencies = graph.GetDependencies("nonexistent").ToList();

        // Assert
        Assert.Empty(dependencies);
    }

    [Fact]
    public void ComplexDependencyGraph_ShouldHandleCorrectly()
    {
        // Arrange
        var graph = CreateDependencyGraph();
        
        // Create a complex dependency graph
        // A -> B, C
        // B -> D, E
        // C -> F
        // D -> G
        // E -> G
        // F -> G
        graph.AddDependency("A", "B");
        graph.AddDependency("A", "C");
        graph.AddDependency("B", "D");
        graph.AddDependency("B", "E");
        graph.AddDependency("C", "F");
        graph.AddDependency("D", "G");
        graph.AddDependency("E", "G");
        graph.AddDependency("F", "G");

        // Act
        var topologicalOrder = graph.GetTopologicalOrder().ToList();
        var fieldsToRecalculate = graph.GetFieldsToRecalculate("G").ToList();
        var stats = graph.GetStats();

        // Assert
        Assert.Equal(7, stats.TotalFields);
        Assert.Equal(8, stats.TotalDependencies);
        Assert.False(stats.HasCircularDependencies);
        
        // G should be first in topological order (no dependencies)
        Assert.Equal("G", topologicalOrder.First());
        
        // A should be last in topological order (depends on everything)
        Assert.Equal("A", topologicalOrder.Last());
        
        // When G changes, all other fields should be recalculated
        Assert.Equal(6, fieldsToRecalculate.Count);
        Assert.Contains("A", fieldsToRecalculate);
        Assert.Contains("B", fieldsToRecalculate);
        Assert.Contains("C", fieldsToRecalculate);
    }
}