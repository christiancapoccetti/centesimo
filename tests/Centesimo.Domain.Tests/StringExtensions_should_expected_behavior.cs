using Centesimo.Domain;

namespace Centesimo.Domain.Tests;

public sealed class StringExtensions_should_expected_behavior
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Treat_null_empty_and_whitespace_as_empty(string? value)
    {
        Assert.True(value.IsEmpty());
        Assert.False(value.HasValue());
    }

    [Fact]
    public void Treat_non_whitespace_text_as_having_value()
    {
        const string value = "Centesimo";

        Assert.False(value.IsEmpty());
        Assert.True(value.HasValue());
    }
}
