namespace Centesimo.Application;

public sealed record ExpenseSpeechDraft(
    long AmountCents,
    Guid CategoryId,
    string CategoryName,
    Guid? TagId,
    string TagName,
    DateOnly? OccurredOn,
    string Note,
    string Transcription);
