namespace Centesimo.Application.Tests;

public sealed class ExpenseSpeechDraftResolver_should_expected_behavior
{
    private readonly ExpenseSpeechDraftResolver _resolver = new();

    [Fact]
    public void Resolve_returns_canonical_ids_for_a_normalized_name_match()
    {
        var categoryId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var command = new ExpenseSpeechCommand(50m, "spese auto", "tagliando", null, "", "testo");
        var result = _resolver.Resolve(command, [new SpeechCategory(categoryId, "Spése Auto", [new SpeechTag(tagId, "Tagliando")])]);

        Assert.True(result.IsSuccess);
        Assert.Equal(5_000, result.Value.AmountCents);
        Assert.Equal(categoryId, result.Value.CategoryId);
        Assert.Equal(tagId, result.Value.TagId);
    }

    [Fact]
    public void Return_a_fallback_draft_for_an_ambiguous_category()
    {
        var command = new ExpenseSpeechCommand(50m, "auto", "", null, "", "testo");
        var result = _resolver.Resolve(command, [new SpeechCategory(Guid.NewGuid(), "Auto", []), new SpeechCategory(Guid.NewGuid(), "AUTO", [])]);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CategoryId);
        Assert.Null(result.Value.TagId);
        Assert.Equal(5_000, result.Value.AmountCents);
    }

    [Fact]
    public void Return_a_fallback_draft_when_the_tag_does_not_match()
    {
        var categoryId = Guid.NewGuid();
        var command = new ExpenseSpeechCommand(50m, "auto", "tagliando", null, "nota", "testo");
        var result = _resolver.Resolve(command, [new SpeechCategory(categoryId, "Auto", [])]);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CategoryId);
        Assert.Null(result.Value.TagId);
        Assert.Equal("nota", result.Value.Note);
    }

    [Fact]
    public void Reject_amount_that_overflows_cents()
    {
        var command = new ExpenseSpeechCommand(decimal.MaxValue, "auto", "", null, "", "testo");
        var result = _resolver.Resolve(command, [new SpeechCategory(Guid.NewGuid(), "Auto", [])]);

        Assert.True(result.IsFailure);
        Assert.Equal("Speech.InvalidAmount", result.Error.Code);
    }
}
