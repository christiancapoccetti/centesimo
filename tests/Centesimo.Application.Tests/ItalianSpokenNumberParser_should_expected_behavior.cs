namespace Centesimo.Application.Tests;

public sealed class ItalianSpokenNumberParser_should_expected_behavior
{
    private readonly ItalianSpokenNumberParser _parser = new();

    [Theory]
    [InlineData("cinquanta euro", 50)]
    [InlineData("centoventi euro", 120)]
    [InlineData("mille duecento euro", 1200)]
    [InlineData("duemila euro", 2000)]
    [InlineData("cinquanta euro e trentadue", 50.32)]
    public void Parse_compositional_italian_amount(string text, decimal expected)
    {
        Assert.True(_parser.TryParse(text, out var actual));
        Assert.Equal(expected, actual);
    }
}
