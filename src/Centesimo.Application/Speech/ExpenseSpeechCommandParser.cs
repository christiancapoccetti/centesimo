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
        if (!category.Success)
            return Result<ExpenseSpeechCommand>.Failure(new Error("Speech.InvalidCommand", "Di' ad esempio: aggiungi spesa di 50 euro alla categoria auto."));

        var phrase = MoneyWithCentsPattern().Match(normalized);
        var spoken = AmountPhrasePattern().Match(normalized);
        var amountText = phrase.Success ? phrase.Groups["amount"].Value : amount.Success ? amount.Groups["amount"].Value : spoken.Groups["amount"].Value;
        if (!TryParseAmount(amountText, out var value) || value <= 0)
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

    [GeneratedRegex(@"(?:(?:di|da)\s+)?(?<amount>\d+(?:[,.]\d{1,2})?)\s*(?:€|euro|eur)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AmountPattern();

    [GeneratedRegex(@"(?<amount>(?:zero|uno|due|tre|quattro|cinque|sei|sette|otto|nove|dieci|undici|dodici|tredici|quattordici|quindici|sedici|diciassette|diciotto|diciannove|venti|trenta|quaranta|cinquanta|sessanta|settanta|ottanta|novanta|cento|mille|mila|\s)+\s*(?:euro|eur|€)(?:\s+(?:e|virgola)\s+(?:\w+|\d+))?)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AmountPhrasePattern();

    [GeneratedRegex(@"(?<amount>(?:\d+|[a-zàèéìòù]+)\s*(?:euro|eur|€)\s+e\s+(?:\d+|[a-zàèéìòù]+))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MoneyWithCentsPattern();

    [GeneratedRegex(@"(?:(?:alla|nella|in)\s+categoria|categoria|spesa\s+(?:su|in)|(?:su|in))\s+(?<category>.+?)(?=\s+(?:sotto\s+)?tag\s+|\s+con\s+nota\s+|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CategoryPattern();

    [GeneratedRegex(@"(?:sotto\s+)?tag\s+(?<tag>.+?)(?=\s+con\s+nota\s+|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"con\s+nota\s+(?<note>.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NotePattern();

    private static string TrimPunctuation(string value) => value.Trim().TrimEnd(',', '.', ';', ':');

    private static bool TryParseAmount(string text, out decimal value)
    {
        var money = Regex.Match(text, @"^(?<whole>.+?)\s*(?:euro|eur|€)(?:\s+e\s+(?<cents>.+))?$", RegexOptions.IgnoreCase);
        if (money.Success && TryParseAmount(money.Groups["whole"].Value, out var whole))
        {
            if (!money.Groups["cents"].Success) { value = whole; return true; }
            if (TryParseAmount(money.Groups["cents"].Value, out var cents) && cents is >= 0 and <= 99) { value = whole + cents / 100m; return true; }
        }
        if (decimal.TryParse(text.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            return true;

        var recognized = Microsoft.Recognizers.Text.Number.NumberRecognizer.RecognizeNumber(text, Microsoft.Recognizers.Text.Culture.Italian);
        var resolution = recognized.FirstOrDefault()?.Resolution;
        if (resolution is not null && resolution.TryGetValue("value", out var recognizedValue) && decimal.TryParse(recognizedValue?.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value)) return true;
        var normalized = text.Trim().ToLowerInvariant().Replace(" ", "");
        var values = new Dictionary<string, decimal>
        {
            ["zero"] = 0, ["uno"] = 1, ["due"] = 2, ["tre"] = 3, ["quattro"] = 4, ["cinque"] = 5,
            ["dieci"] = 10, ["venti"] = 20, ["cinquanta"] = 50, ["cento"] = 100, ["centoventi"] = 120,
            ["mille"] = 1000, ["milleduecento"] = 1200
        };
        return values.TryGetValue(normalized, out value);
    }
}
