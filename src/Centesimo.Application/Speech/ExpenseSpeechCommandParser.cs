using System.Globalization;
using System.Text.RegularExpressions;

namespace Centesimo.Application;

public sealed partial class ExpenseSpeechCommandParser
{
    public Result<ExpenseSpeechCommand> Parse(string transcription)
    {
        if (string.IsNullOrWhiteSpace(transcription))
            return Result<ExpenseSpeechCommand>.Failure(new Error("Speech.Empty", "Non ho sentito alcun comando."));

        var normalized = transcription.Trim();
        var amount = AmountPattern().Match(normalized);
        var category = CategoryPattern().Match(normalized);
        if (!amount.Success || !category.Success)
            return Result<ExpenseSpeechCommand>.Failure(new Error("Speech.InvalidCommand", "Di' ad esempio: aggiungi spesa di 50 euro alla categoria auto."));

        var amountText = amount.Groups["amount"].Value.Replace(',', '.');
        if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) || value <= 0)
            return Result<ExpenseSpeechCommand>.Failure(new Error("Speech.InvalidAmount", "L'importo non è valido."));

        var tag = TagPattern().Match(normalized);
        var note = NotePattern().Match(normalized);
        return Result<ExpenseSpeechCommand>.Success(new ExpenseSpeechCommand(
            value,
            TrimPunctuation(category.Groups["category"].Value),
            tag.Success ? TrimPunctuation(tag.Groups["tag"].Value) : "",
            null,
            note.Success ? note.Groups["note"].Value.Trim() : "",
            normalized));
    }

    [GeneratedRegex(@"(?:di|da)\s+(?<amount>\d+(?:[,.]\d{1,2})?)\s*(?:€|euro|eur)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AmountPattern();

    [GeneratedRegex(@"(?:alla|nella|in)\s+categoria\s+(?<category>.+?)(?=\s+(?:sotto\s+)?tag\s+|\s+con\s+nota\s+|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CategoryPattern();

    [GeneratedRegex(@"(?:sotto\s+)?tag\s+(?<tag>.+?)(?=\s+con\s+nota\s+|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"con\s+nota\s+(?<note>.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NotePattern();

    private static string TrimPunctuation(string value) => value.Trim().TrimEnd(',', '.', ';', ':');
}
