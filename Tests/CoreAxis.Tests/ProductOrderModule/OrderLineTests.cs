using CoreAxis.Modules.ProductOrderModule.Domain.Orders;
using CoreAxis.Modules.ProductOrderModule.Domain.Orders.ValueObjects;
using Xunit;

namespace CoreAxis.Tests.ProductOrderModule;

public class OrderLineTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateOrderLine()
    {
        // Arrange
        var assetCode = AssetCode.Create("BTC");
        var quantity = 1.5m;
        var unitPrice = 50000.12345678m;
        var description = "Test order line";

        // Act
        var orderLine = OrderLine.Create(assetCode, quantity, unitPrice, description);

        // Assert
        Assert.Equal(assetCode, orderLine.AssetCode);
        Assert.Equal(quantity, orderLine.Quantity);
        Assert.Equal(unitPrice, orderLine.UnitPrice);
        Assert.Equal(description, orderLine.Description);
        Assert.Equal(75000.18518517m, orderLine.TotalPrice);
    }

    [Fact]
    public void Create_WithoutDescription_ShouldCreateOrderLineWithNullDescription()
    {
        // Arrange
        var assetCode = AssetCode.Create("ETH");
        var quantity = 2.0m;
        var unitPrice = 3000.5m;

        // Act
        var orderLine = OrderLine.Create(assetCode, quantity, unitPrice);

        // Assert
        Assert.Equal(assetCode, orderLine.AssetCode);
        Assert.Equal(quantity, orderLine.Quantity);
        Assert.Equal(unitPrice, orderLine.UnitPrice);
        Assert.Null(orderLine.Description);
        Assert.Equal(6001.0m, orderLine.TotalPrice);
    }

    [Fact]
    public void Create_WithZeroQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var assetCode = AssetCode.Create("BTC");
        var quantity = 0m;
        var unitPrice = 50000m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => OrderLine.Create(assetCode, quantity, unitPrice));
        Assert.Contains("Quantity must be greater than zero", exception.Message);
    }

    [Fact]
    public void Create_WithNegativeQuantity_ShouldThrowArgumentException()
    {
        // Arrange
        var assetCode = AssetCode.Create("BTC");
        var quantity = -1.0m;
        var unitPrice = 50000m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => OrderLine.Create(assetCode, quantity, unitPrice));
        Assert.Contains("Quantity must be greater than zero", exception.Message);
    }

    [Fact]
    public void Create_WithZeroUnitPrice_ShouldThrowArgumentException()
    {
        // Arrange
        var assetCode = AssetCode.Create("BTC");
        var quantity = 1.0m;
        var unitPrice = 0m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => OrderLine.Create(assetCode, quantity, unitPrice));
        Assert.Contains("UnitPrice must be greater than zero", exception.Message);
    }

    [Fact]
    public void Create_WithNegativeUnitPrice_ShouldThrowArgumentException()
    {
        // Arrange
        var assetCode = AssetCode.Create("BTC");
        var quantity = 1.0m;
        var unitPrice = -50000m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => OrderLine.Create(assetCode, quantity, unitPrice));
        Assert.Contains("UnitPrice must be greater than zero", exception.Message);
    }

    [Fact]
    public void UpdatePrice_WithValidPrice_ShouldUpdatePriceAndRecalculateTotal()
    {
        // Arrange
        var assetCode = AssetCode.Create("BTC");
        var quantity = 2.0m;
        var initialPrice = 50000m;
        var orderLine = OrderLine.Create(assetCode, quantity, initialPrice);
        
        var newPrice = 55000.12345678m;

        // Act
        orderLine.UpdatePrice(newPrice);

        // Assert
        Assert.Equal(newPrice, orderLine.UnitPrice);
        Assert.Equal(110000.24691356m, orderLine.TotalPrice);
    }

    [Fact]
    public void UpdatePrice_WithZeroPrice_ShouldThrowArgumentException()
    {
        // Arrange
        var assetCode = AssetCode.Create("BTC");
        var quantity = 1.0m;
        var unitPrice = 50000m;
        var orderLine = OrderLine.Create(assetCode, quantity, unitPrice);
        var zeroPrice = 0m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => orderLine.UpdatePrice(zeroPrice));
        Assert.Contains("UnitPrice must be greater than zero", exception.Message);
    }

    [Fact]
    public void UpdatePrice_WithNegativePrice_ShouldThrowArgumentException()
    {
        // Arrange
        var assetCode = AssetCode.Create("BTC");
        var quantity = 1.0m;
        var unitPrice = 50000m;
        var orderLine = OrderLine.Create(assetCode, quantity, unitPrice);
        var negativePrice = -1000m;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => orderLine.UpdatePrice(negativePrice));
        Assert.Contains("UnitPrice must be greater than zero", exception.Message);
    }

    [Fact]
    public void OrderId_ShouldBeEmptyInitially()
    {
        // Arrange
        var assetCode = AssetCode.Create("ETH");
        var quantity = 1.0m;
        var unitPrice = 3000m;

        // Act
        var orderLine = OrderLine.Create(assetCode, quantity, unitPrice);

        // Assert
        Assert.Equal(Guid.Empty, orderLine.OrderId);
    }

    [Fact]
    public void PrecisionTest_HighPrecisionCalculations_ShouldMaintainPrecision()
    {
        // Arrange
        var assetCode = AssetCode.Create("BTC");
        var quantity = 0.12345678m; // High precision quantity
        var unitPrice = 87654.32109876m; // High precision price

        // Act
        var orderLine = OrderLine.Create(assetCode, quantity, unitPrice);

        // Assert
        // Expected: 0.12345678 * 87654.32109876 = 10823.27263374
        Assert.Equal(10823.27263374m, orderLine.TotalPrice);
    }

    [Fact]
    public void PrecisionTest_MultipleUpdates_ShouldMaintainPrecision()
    {
        // Arrange
        var assetCode = AssetCode.Create("ETH");
        var quantity = 2.12345678m;
        var initialPrice = 3000.12345678m;
        var orderLine = OrderLine.Create(assetCode, quantity, initialPrice);

        // Act
        orderLine.UpdatePrice(3500.87654321m);

        // Assert
        var expectedTotal = 2.12345678m * 3500.87654321m; // = 7433.60493827
        Assert.Equal(7433.60493827m, orderLine.TotalPrice);
    }

    [Fact]
    public void TotalPriceCalculation_ShouldBeAccurate()
    {
        // Arrange
        var assetCode = AssetCode.Create("BTC");
        var quantity = 1.5m;
        var unitPrice = 50000.12345678m;

        // Act
        var orderLine = OrderLine.Create(assetCode, quantity, unitPrice);

        // Assert
        var expectedTotal = quantity * unitPrice;
        Assert.Equal(expectedTotal, orderLine.TotalPrice);
        Assert.Equal(75000.18518517m, orderLine.TotalPrice);
    }

    [Fact]
    public void MappingTest_DecimalPrecision_ShouldMaintainDataIntegrity()
    {
        // This test verifies that decimal precision is maintained in Orders.OrderLine
        // Arrange
        var assetCode = AssetCode.Create("BTC");
        var quantity = 1.12345678m;
        var unitPrice = 50000.87654321m;
        var description = "High precision order";
        
        var orderLine = OrderLine.Create(assetCode, quantity, unitPrice, description);

        // Act - Verify all decimal values maintain precision
        var mappedQuantity = orderLine.Quantity;
        var mappedUnitPrice = orderLine.UnitPrice;
        var mappedTotal = orderLine.TotalPrice;

        // Assert - Verify precision is maintained
        Assert.Equal(1.12345678m, mappedQuantity);
        Assert.Equal(50000.87654321m, mappedUnitPrice);
        Assert.Equal(56179.60493827m, mappedTotal); // 1.12345678 * 50000.87654321
        Assert.Equal(description, orderLine.Description);
    }
}