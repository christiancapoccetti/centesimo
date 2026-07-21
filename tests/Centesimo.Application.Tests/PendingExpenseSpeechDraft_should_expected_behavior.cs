namespace Centesimo.Application.Tests;

public sealed class PendingExpenseSpeechDraft_should_expected_behavior
{
    [Fact]
    public void Take_returns_draft_only_once()
    {
        var pending = new PendingExpenseSpeechDraft();
        var draft = new ExpenseSpeechDraft(5_000, "Auto", "", null, "", "testo");

        pending.Set(draft);

        Assert.Same(draft, pending.Take());
        Assert.Null(pending.Take());
    }
}
