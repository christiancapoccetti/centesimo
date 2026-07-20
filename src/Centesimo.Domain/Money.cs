namespace Centesimo.Domain;

public readonly record struct Money
{
    public long Cents { get; }

    public Money(long cents)
    {
        if (cents < 0)
            throw new ArgumentOutOfRangeException(nameof(cents), "Money cannot be negative.");

        Cents = cents;
    }

    public decimal ToDecimal() => Cents / 100m;

    public static Money FromDecimal(decimal amount)
    {
        if (decimal.Round(amount, 2) != amount)
            throw new ArgumentException("Money cannot have more than two decimal places.", nameof(amount));

        return new Money(checked((long)(amount * 100m)));
    }
}
