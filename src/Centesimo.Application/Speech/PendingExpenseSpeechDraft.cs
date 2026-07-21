namespace Centesimo.Application;

public interface IPendingExpenseSpeechDraft
{
    void Set(ExpenseSpeechDraft draft);
    ExpenseSpeechDraft? Take();
}

public sealed class PendingExpenseSpeechDraft : IPendingExpenseSpeechDraft
{
    private ExpenseSpeechDraft? _draft;

    public void Set(ExpenseSpeechDraft draft) => _draft = draft;

    public ExpenseSpeechDraft? Take()
    {
        var draft = _draft;
        _draft = null;
        return draft;
    }
}
