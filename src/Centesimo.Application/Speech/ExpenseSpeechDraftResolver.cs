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

        if (!string.IsNullOrWhiteSpace(command.TagName) && SingleMatch(command.TagName, category.Tags, item => item.Name) is null)
            return Result<ExpenseSpeechDraft>.Failure(new Error("Speech.TagNotFound", "Non trovo un tag attivo corrispondente nella categoria selezionata."));

        return Result<ExpenseSpeechDraft>.Success(new ExpenseSpeechDraft(
            decimal.ToInt64(decimal.Round(command.Amount * 100, 0, MidpointRounding.AwayFromZero)),
            category.Name,
            command.TagName,
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

public sealed record SpeechCategory(string Name, IReadOnlyCollection<SpeechTag> Tags);
public sealed record SpeechTag(string Name);
