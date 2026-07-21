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
    public void Reject_ambiguous_category()
    {
        var command = new ExpenseSpeechCommand(50m, "auto", "", null, "", "testo");
        var result = _resolver.Resolve(command, [new SpeechCategory(Guid.NewGuid(), "Auto", []), new SpeechCategory(Guid.NewGuid(), "AUTO", [])]);

        Assert.True(result.IsFailure);
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
