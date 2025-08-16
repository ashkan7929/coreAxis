using CoreAxis.SharedKernel;

namespace CoreAxis.Modules.ProductOrderModule.Domain.ValueObjects;

/// <summary>
/// Value object representing a monetary amount with currency.
/// Supports high precision decimal calculations (18,8).
/// </summary>
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Creates a new Money instance.
    /// </summary>
    /// <param name="amount">The monetary amount</param>
    /// <param name="currency">The currency code (e.g., "USD", "BTC")</param>
    /// <returns>A new Money instance</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be null or empty.", nameof(currency));

        if (currency.Length < 2 || currency.Length > 10)
            throw new ArgumentException("Currency must be between 2 and 10 characters.", nameof(currency));

        return new Money(amount, currency.ToUpperInvariant());
    }

    /// <summary>
    /// Creates a zero amount for the specified currency.
    /// </summary>
    /// <param name="currency">The currency code</param>
    /// <returns>A Money instance with zero amount</returns>
    public static Money Zero(string currency) => Create(0, currency);

    /// <summary>
    /// Adds two Money instances of the same currency.
    /// </summary>
    /// <param name="left">First Money instance</param>
    /// <param name="right">Second Money instance</param>
    /// <returns>Sum of the two amounts</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match</exception>
    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot add different currencies: {left.Currency} and {right.Currency}");

        return Create(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>
    /// Subtracts two Money instances of the same currency.
    /// </summary>
    /// <param name="left">First Money instance</param>
    /// <param name="right">Second Money instance</param>
    /// <returns>Difference of the two amounts</returns>
    /// <exception cref="InvalidOperationException">Thrown when currencies don't match</exception>
    public static Money operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException($"Cannot subtract different currencies: {left.Currency} and {right.Currency}");

        return Create(left.Amount - right.Amount, left.Currency);
    }

    /// <summary>
    /// Multiplies Money by a decimal factor.
    /// </summary>
    /// <param name="money">Money instance</param>
    /// <param name="factor">Multiplication factor</param>
    /// <returns>Multiplied amount</returns>
    public static Money operator *(Money money, decimal factor)
    {
        return Create(money.Amount * factor, money.Currency);
    }

    /// <summary>
    /// Checks if the amount is zero.
    /// </summary>
    public bool IsZero => Amount == 0;

    /// <summary>
    /// Checks if the amount is positive.
    /// </summary>
    public bool IsPositive => Amount > 0;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:F8} {Currency}";
}