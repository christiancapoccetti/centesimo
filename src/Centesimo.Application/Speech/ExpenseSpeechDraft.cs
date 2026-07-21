namespace Centesimo.Application;

public sealed record ExpenseSpeechDraft(
    long AmountCents,
    string CategoryName,
    string TagName,
    DateOnly? OccurredOn,
    string Note,
    string Transcription);
