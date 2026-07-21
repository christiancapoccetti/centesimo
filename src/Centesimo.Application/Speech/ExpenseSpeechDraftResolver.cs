using System.Globalization;
using System.Text;

namespace Centesimo.Application;

public sealed class ExpenseSpeechDraftResolver
{
    public Result<ExpenseSpeechDraft> Resolve(ExpenseSpeechCommand command, IEnumerable<SpeechCategory> categories)
    {
        var category = SingleMatch(command.CategoryName, categories, item => item.Name);
        if (category is null)
            return Result<ExpenseSpeechDraft>.Failure(new Error("Speech.CategoryNotFound", "Non trovo una categoria attiva corrispondente."));

        var tag = string.IsNullOrWhiteSpace(command.TagName)
            ? null
            : SingleMatch(command.TagName, category.Tags, item => item.Name);
        if (!string.IsNullOrWhiteSpace(command.TagName) && tag is null)
            return Result<ExpenseSpeechDraft>.Failure(new Error("Speech.TagNotFound", "Non trovo un tag attivo corrispondente nella categoria selezionata."));

        if (command.Amount <= 0 || command.Amount > long.MaxValue / 100m)
            return Result<ExpenseSpeechDraft>.Failure(new Error("Speech.InvalidAmount", "L'importo non è valido."));

        var cents = command.Amount * 100;
        if (decimal.Truncate(cents) != cents)
            return Result<ExpenseSpeechDraft>.Failure(new Error("Speech.InvalidAmount", "L'importo non è valido."));

        return Result<ExpenseSpeechDraft>.Success(new ExpenseSpeechDraft(
            decimal.ToInt64(cents),
            category.CategoryId,
            category.Name,
            tag?.TagId,
            tag?.Name ?? "",
            command.OccurredOn,
            command.Note,
            command.Transcription));
    }

    private static T? SingleMatch<T>(string expected, IEnumerable<T> items, Func<T, string> name)
        where T : class
    {
        var matches = items.Where(item => Normalize(name(item)) == Normalize(expected)).Take(2).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }

    private static string Normalize(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));

        return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
    }
}

public sealed record SpeechCategory(Guid CategoryId, string Name, IReadOnlyCollection<SpeechTag> Tags);
public sealed record SpeechTag(Guid TagId, string Name);
