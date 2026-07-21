using Centesimo.Domain;

namespace Centesimo.Application;

public enum InsightPeriod
{
    Month,
    Year
}

public interface IInsightsService
{
    Task<Result<InsightsOverview>> Get(InsightPeriod period, CancellationToken cancellationToken = default);
}

public sealed record InsightCategory(Guid CategoryId, string Name, string Icon, string Color,
    long SpentCents, double Percentage, long? ComparedSpentCents, double? ChangePercentage);

public enum InsightKind { Increased, Decreased, TopCategory, NewHabit, UnusualExpense, Stable }

public sealed record LocalInsight(InsightKind Kind, string Title, string Description, Guid? CategoryId = null,
    Guid? ExpenseId = null);

public sealed record InsightsOverview(InsightPeriod Period, DateOnly From, DateOnly To, long SpentCents,
    long? ComparedSpentCents, double? ChangePercentage, IReadOnlyList<InsightCategory> Categories,
    IReadOnlyList<LocalInsight> Insights);

public sealed class InsightsService(ICategoryRepository categories, IExpenseRepository expenses,
    Func<DateOnly>? today = null) : IInsightsService
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    public async Task<Result<InsightsOverview>> Get(InsightPeriod period, CancellationToken cancellationToken = default)
    {
        var current = _today();
        var (from, to, previousFrom, previousTo) = GetRanges(period, current);
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<InsightsOverview>.Failure(categoryResult.Error);

        var currentResult = await expenses.GetBetween(from, to, cancellationToken);
        if (currentResult.IsFailure)
            return Result<InsightsOverview>.Failure(currentResult.Error);

        var previousResult = previousFrom.HasValue
            ? await expenses.GetBetween(previousFrom.Value, previousTo!.Value, cancellationToken)
            : Result<IReadOnlyList<Expense>>.Success([]);
        if (previousResult.IsFailure)
            return Result<InsightsOverview>.Failure(previousResult.Error);

        var activeCategories = categoryResult.Value.Where(x => !x.IsArchived).ToDictionary(x => x.CategoryId);
        var currentExpenses = currentResult.Value;
        var previousExpenses = previousResult.Value;
        var total = currentExpenses.Sum(x => x.Amount.Cents);
        var previousTotal = previousExpenses.Sum(x => x.Amount.Cents);
        long? comparable = previousTotal > 0 ? previousTotal : null;
        var comparisonCategories = currentExpenses.Concat(previousExpenses)
            .GroupBy(x => x.CategoryId)
            .Where(x => activeCategories.ContainsKey(x.Key))
            .Select(group =>
            {
                var category = activeCategories[group.Key];
                var spent = currentExpenses.Where(x => x.CategoryId == group.Key).Sum(x => x.Amount.Cents);
                var prior = previousExpenses.Where(x => x.CategoryId == group.Key).Sum(x => x.Amount.Cents);
                return new InsightCategory(category.CategoryId, category.Name, category.Icon, category.Color, spent,
                    total == 0 ? 0 : (double)spent / total, prior == 0 ? null : prior,
                    prior == 0 ? null : ((double)(spent - prior) / prior));
            })
            .OrderByDescending(x => x.SpentCents).ThenBy(x => x.Name).ToList();
        var categoryItems = comparisonCategories.Where(x => x.SpentCents > 0).ToList();
        var localInsights = BuildInsights(comparisonCategories, currentExpenses, previousExpenses, total, comparable);
        return Result<InsightsOverview>.Success(new InsightsOverview(period, from, to, total, comparable,
            comparable is null ? null : (double)(total - comparable.Value) / comparable.Value,
            categoryItems, localInsights));
    }

    private static (DateOnly From, DateOnly To, DateOnly? PreviousFrom, DateOnly? PreviousTo) GetRanges(InsightPeriod period, DateOnly today)
    {
        if (period == InsightPeriod.Month)
        {
            var from = new DateOnly(today.Year, today.Month, 1);
            var previousFrom = from.AddMonths(-1);
            var previousTo = previousFrom.AddMonths(1).AddDays(-1);
            if (today.Day > previousTo.Day)
                return (from, today, null, null);
            return (from, today, previousFrom, previousFrom.AddDays(today.Day - 1));
        }

        var yearStart = new DateOnly(today.Year, 1, 1);
        var previousYearStart = yearStart.AddYears(-1);
        if (today.Month == 2 && today.Day == 29 && !DateTime.IsLeapYear(today.Year - 1))
            return (yearStart, today, null, null);
        var previousYearTo = new DateOnly(today.Year - 1, today.Month, today.Day);
        return (yearStart, today, previousYearStart, previousYearTo);
    }

    private static IReadOnlyList<LocalInsight> BuildInsights(IReadOnlyList<InsightCategory> categories,
        IReadOnlyList<Expense> currentExpenses, IReadOnlyList<Expense> previousExpenses, long total, long? previousTotal)
    {
        if (total == 0)
            return [];

        var insights = new List<LocalInsight>();
        var increased = categories.FirstOrDefault(x => x.ChangePercentage >= .1);
        if (increased is not null)
            insights.Add(new LocalInsight(InsightKind.Increased, "Spese in aumento", $"Hai speso di più per {increased.Name} rispetto al periodo precedente.", increased.CategoryId));
        var decreased = categories.FirstOrDefault(x => x.ChangePercentage <= -.1);
        if (decreased is not null)
            insights.Add(new LocalInsight(InsightKind.Decreased, "Spese in diminuzione", $"Hai speso di meno per {decreased.Name} rispetto al periodo precedente.", decreased.CategoryId));
        if (categories.Count > 0)
        {
            var top = categories[0];
            insights.Add(new LocalInsight(InsightKind.TopCategory, "La categoria principale", $"{top.Name} rappresenta il {top.Percentage:P0} delle spese del periodo.", top.CategoryId));
        }
        var newHabit = currentExpenses
            .GroupBy(x => x.CategoryId)
            .FirstOrDefault(group => group.Count() >= 3 && !previousExpenses.Any(x => x.CategoryId == group.Key));
        if (newHabit is not null && categories.FirstOrDefault(x => x.CategoryId == newHabit.Key) is { } newCategory)
            insights.Add(new LocalInsight(InsightKind.NewHabit, "Una nuova abitudine", $"Hai registrato {newHabit.Count()} spese in {newCategory.Name} in questo periodo.", newCategory.CategoryId));
        var unusual = currentExpenses
            .GroupBy(x => x.CategoryId)
            .Select(group => new { Expenses = group.ToList(), Maximum = group.MaxBy(x => x.Amount.Cents) })
            .FirstOrDefault(group => group.Expenses.Count >= 2 && group.Maximum!.Amount.Cents >= group.Expenses.Average(x => x.Amount.Cents) * 1.8);
        if (unusual?.Maximum is not null && categories.FirstOrDefault(x => x.CategoryId == unusual.Maximum.CategoryId) is { } unusualCategory)
            insights.Add(new LocalInsight(InsightKind.UnusualExpense, "Una spesa da rivedere", $"{unusual.Maximum.Amount.Cents / 100m:0.00} € per {unusualCategory.Name} è sensibilmente più alta delle altre spese della categoria.", unusualCategory.CategoryId, unusual.Maximum.ExpenseId));
        if (previousTotal > 0 && Math.Abs((double)(total - previousTotal.Value) / previousTotal.Value) < .1)
            insights.Add(new LocalInsight(InsightKind.Stable, "Spese stabili", "Il totale è simile al periodo precedente."));
        return insights
            .OrderBy(x => x.Kind is InsightKind.UnusualExpense or InsightKind.NewHabit ? 0 : 1)
            .ToList();
    }
}
