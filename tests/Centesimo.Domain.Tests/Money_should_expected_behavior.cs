using Centesimo.Domain;

namespace Centesimo.Domain.Tests;

public sealed class Money_should_expected_behavior
{
    [Fact]
    public void Store_amount_as_integer_cents()
    {
        var money = Money.FromDecimal(12.34m);

        Assert.Equal(1234, money.Cents);
        Assert.Equal(12.34m, money.ToDecimal());
    }

    [Fact]
    public void Reject_negative_amounts() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(-1));

    [Fact]
    public void Reject_more_than_two_decimal_places() =>
        Assert.Throws<ArgumentException>(() => Money.FromDecimal(1.001m));
}
