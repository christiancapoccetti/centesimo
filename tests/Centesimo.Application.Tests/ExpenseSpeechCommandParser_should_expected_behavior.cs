namespace Centesimo.Application.Tests;

public sealed class ExpenseSpeechCommandParser_should_expected_behavior
{
    private readonly ExpenseSpeechCommandParser _parser = new();

    [Fact]
    public void Parse_italian_expense_command()
    {
        var result = _parser.Parse("Aggiungi spesa di 50 eur alla categoria spese auto, sotto tag tagliando");

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value.Amount);
        Assert.Equal("spese auto", result.Value.CategoryName);
        Assert.Equal("tagliando", result.Value.TagName);
    }

    [Fact]
    public void Reject_command_without_required_fields()
    {
        var result = _parser.Parse("aggiungi una spesa");

        Assert.True(result.IsFailure);
    }
}
