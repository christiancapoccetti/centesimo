using System.Globalization;

namespace Centesimo.Application.Tests;

public sealed class MoneyInputParser_should_expected_behavior
{
    [Fact]
    public void Parse_optional_italian_decimal_to_cents()
    {
        var result = MoneyInputParser.ParseOptional("123,45", CultureInfo.GetCultureInfo("it-IT"));

        Assert.Equal(12_345, result.Value?.Cents);
    }

    [Fact]
    public void Treat_blank_budget_as_missing() =>
        Assert.Null(MoneyInputParser.ParseOptional(" ", CultureInfo.InvariantCulture).Value);

    [Theory]
    [InlineData("-1")]
    [InlineData("1.001")]
    [InlineData("abc")]
    public void Reject_invalid_budget(string value) =>
        Assert.Equal("Money.InvalidInput",
            MoneyInputParser.ParseOptional(value, CultureInfo.InvariantCulture).Error.Code);
}
