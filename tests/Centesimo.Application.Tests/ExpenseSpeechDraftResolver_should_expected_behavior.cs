namespace Centesimo.Application.Tests;

public sealed class ExpenseSpeechDraftResolver_should_expected_behavior
{
    private readonly ExpenseSpeechDraftResolver _resolver = new();

    [Fact]
    public void Resolve_matches_names_without_case_or_diacritics()
    {
        var command = new ExpenseSpeechCommand(50m, "spese auto", "tagliando", null, "", "testo");
        var result = _resolver.Resolve(command, [new SpeechCategory("Spése Auto", [new SpeechTag("Tagliando")])]);

        Assert.True(result.IsSuccess);
        Assert.Equal(5_000, result.Value.AmountCents);
        Assert.Equal("Spése Auto", result.Value.CategoryName);
    }

    [Fact]
    public void Reject_ambiguous_category()
    {
        var command = new ExpenseSpeechCommand(50m, "auto", "", null, "", "testo");
        var result = _resolver.Resolve(command, [new SpeechCategory("Auto", []), new SpeechCategory("AUTO", [])]);

        Assert.True(result.IsFailure);
    }
}
