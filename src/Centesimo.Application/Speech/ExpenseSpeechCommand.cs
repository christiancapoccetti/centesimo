namespace Centesimo.Application;

public sealed record ExpenseSpeechCommand(
    decimal Amount,
    string CategoryName,
    string TagName,
    DateOnly? OccurredOn,
    string Note,
    string Transcription);
