using CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;
using Xunit;

namespace CoreAxis.Tests.ProductOrderModule;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidAmountAndCurrency_ShouldCreateMoney()
    {
        // Arrange
        var amount = 100.50m;
        var currency = "USD";

        // Act
        var money = Money.Create(amount, currency);

        // Assert
        Assert.Equal(amount, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Arrange
        var amount = -10.50m;
        var currency = "USD";

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Money.Create(amount, currency));
        Assert.Contains("Amount cannot be negative", exception.Message);
    }

    [Fact]
    public void Create_WithNullCurrency_ShouldThrowArgumentException()
    {
        // Arrange
        var amount = 100.50m;
        string currency = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Money.Create(amount, currency));
        Assert.Contains("Currency cannot be null or empty", exception.Message);
    }

    [Fact]
    public void Create_WithInvalidCurrencyLength_ShouldThrowArgumentException()
    {
        // Arrange
        var amount = 100.50m;
        var currency = "A"; // Too short

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Money.Create(amount, currency));
        Assert.Contains("Currency must be between 2 and 10 characters", exception.Message);
    }

    [Theory]
    [InlineData("usd", "USD")]
    [InlineData("btc", "BTC")]
    [InlineData("eth", "ETH")]
    public void Create_WithLowerCaseCurrency_ShouldConvertToUpperCase(string input, string expected)
    {
        // Arrange
        var amount = 100.50m;

        // Act
        var money = Money.Create(amount, input);

        // Assert
        Assert.Equal(expected, money.Currency);
    }

    [Fact]
    public void Zero_ShouldCreateZeroAmount()
    {
        // Arrange
        var currency = "USD";

        // Act
        var money = Money.Zero(currency);

        // Assert
        Assert.Equal(0, money.Amount);
        Assert.Equal("USD", money.Currency);
        Assert.True(money.IsZero);
    }

    [Fact]
    public void Addition_WithSameCurrency_ShouldReturnSum()
    {
        // Arrange
        var money1 = Money.Create(100.50m, "USD");
        var money2 = Money.Create(50.25m, "USD");

        // Act
        var result = money1 + money2;

        // Assert
        Assert.Equal(150.75m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Addition_WithDifferentCurrency_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = Money.Create(100.50m, "USD");
        var money2 = Money.Create(50.25m, "EUR");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => money1 + money2);
        Assert.Contains("Cannot add different currencies", exception.Message);
    }

    [Fact]
    public void Subtraction_WithSameCurrency_ShouldReturnDifference()
    {
        // Arrange
        var money1 = Money.Create(100.50m, "USD");
        var money2 = Money.Create(50.25m, "USD");

        // Act
        var result = money1 - money2;

        // Assert
        Assert.Equal(50.25m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Subtraction_WithDifferentCurrency_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var money1 = Money.Create(100.50m, "USD");
        var money2 = Money.Create(50.25m, "EUR");

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => money1 - money2);
        Assert.Contains("Cannot subtract different currencies", exception.Message);
    }

    [Fact]
    public void Multiplication_ShouldReturnMultipliedAmount()
    {
        // Arrange
        var money = Money.Create(100.50m, "USD");
        var factor = 2.5m;

        // Act
        var result = money * factor;

        // Assert
        Assert.Equal(251.25m, result.Amount);
        Assert.Equal("USD", result.Currency);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(100.50, false)]
    public void IsZero_ShouldReturnCorrectValue(decimal amount, bool expected)
    {
        // Arrange
        var money = Money.Create(amount, "USD");

        // Act & Assert
        Assert.Equal(expected, money.IsZero);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(100.50, true)]
    public void IsPositive_ShouldReturnCorrectValue(decimal amount, bool expected)
    {
        // Arrange
        var money = Money.Create(amount, "USD");

        // Act & Assert
        Assert.Equal(expected, money.IsPositive);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var money = Money.Create(100.123456789m, "USD");

        // Act
        var result = money.ToString();

        // Assert
        Assert.Equal("100.12345679 USD", result);
    }

    [Fact]
    public void Equality_WithSameAmountAndCurrency_ShouldBeEqual()
    {
        // Arrange
        var money1 = Money.Create(100.50m, "USD");
        var money2 = Money.Create(100.50m, "USD");

        // Act & Assert
        Assert.Equal(money1, money2);
        Assert.True(money1 == money2);
        Assert.False(money1 != money2);
    }

    [Fact]
    public void Equality_WithDifferentAmount_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = Money.Create(100.50m, "USD");
        var money2 = Money.Create(200.50m, "USD");

        // Act & Assert
        Assert.NotEqual(money1, money2);
        Assert.False(money1 == money2);
        Assert.True(money1 != money2);
    }

    [Fact]
    public void Equality_WithDifferentCurrency_ShouldNotBeEqual()
    {
        // Arrange
        var money1 = Money.Create(100.50m, "USD");
        var money2 = Money.Create(100.50m, "EUR");

        // Act & Assert
        Assert.NotEqual(money1, money2);
        Assert.False(money1 == money2);
        Assert.True(money1 != money2);
    }

    [Fact]
    public void PrecisionTest_HighPrecisionDecimal_ShouldMaintainPrecision()
    {
        // Arrange
        var highPrecisionAmount = 123.12345678m; // 8 decimal places
        var currency = "BTC";

        // Act
        var money = Money.Create(highPrecisionAmount, currency);

        // Assert
        Assert.Equal(highPrecisionAmount, money.Amount);
        Assert.Equal("BTC", money.Currency);
    }

    [Fact]
    public void PrecisionTest_ArithmeticOperations_ShouldMaintainPrecision()
    {
        // Arrange
        var money1 = Money.Create(0.12345678m, "BTC");
        var money2 = Money.Create(0.87654322m, "BTC");

        // Act
        var sum = money1 + money2;
        var difference = money2 - money1;
        var product = money1 * 2.5m;

        // Assert
        Assert.Equal(1.00000000m, sum.Amount);
        Assert.Equal(0.75308644m, difference.Amount);
        Assert.Equal(0.30864195m, product.Amount);
    }
}