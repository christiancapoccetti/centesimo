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

    [Theory]
    [InlineData("Inserisci cinquanta euro e trentadue di spesa su lavoro")]
    [InlineData("Inserisci 50 euro e 32 di spesa su lavoro")]
    public void Parse_spoken_or_digit_cents(string transcription)
    {
        var result = _parser.Parse(transcription);
        Assert.True(result.IsSuccess);
        Assert.Equal(50.32m, result.Value.Amount);
    }

    [Fact]
    public void Reject_unknown_spoken_amount_typo() =>
        Assert.True(_parser.Parse("Inserisci cinquata euro di spesa su lavoro").IsFailure);

    [Fact]
    public void Parse_spoken_amount_with_sul_category_connector()
    {
        var result = _parser.Parse("inserisci cinquanta euro di spesa sul lavoro");
        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value.Amount);
        Assert.Equal("lavoro", result.Value.CategoryName);
    }

    [Fact]
    public void Parse_category_and_tag_separated_by_con_tag()
    {
        var result = _parser.Parse("inserisci cinque euro su vacanze con tag caselli");

        Assert.True(result.IsSuccess);
        Assert.Equal(5m, result.Value.Amount);
        Assert.Equal("vacanze", result.Value.CategoryName);
        Assert.Equal("caselli", result.Value.TagName);
    }
}
