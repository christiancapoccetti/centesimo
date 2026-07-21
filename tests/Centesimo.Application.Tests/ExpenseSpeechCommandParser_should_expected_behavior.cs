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

    [Theory]
    [InlineData("Inserisci 50 euro di spesa su lavoro", "lavoro")]
    [InlineData("Registra 50 euro di spesa in Lavoro", "Lavoro")]
    [InlineData("Aggiungi spesa di 50 euro categoria lavoro", "lavoro")]
    public void Parse_flexible_italian_category_connectors(string transcription, string category)
    {
        var result = _parser.Parse(transcription);

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value.Amount);
        Assert.Equal(category, result.Value.CategoryName);
    }

    [Theory]
    [InlineData("Inserisci cinquanta euro di spesa su lavoro", 50)]
    [InlineData("Aggiungi centoventi euro alla categoria lavoro", 120)]
    public void Parse_common_italian_spoken_amounts(string transcription, decimal amount)
    {
        var result = _parser.Parse(transcription);
        Assert.True(result.IsSuccess);
        Assert.Equal(amount, result.Value.Amount);
    }
}
