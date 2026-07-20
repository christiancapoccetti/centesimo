using Centesimo.Domain;

namespace Centesimo.Application;

public sealed record TodayCategoryOverview(Guid CategoryId, string Name, string Icon, string Color,
    long SpentCents, long? BudgetCents);

public sealed record TodayExpenseOverview(Guid ExpenseId, string CategoryName, string CategoryIcon,
    string CategoryColor, long AmountCents, string Note);

public sealed record TodayOverview(long MonthlySpentCents, long? TotalBudgetCents,
    IReadOnlyList<TodayCategoryOverview> Categories, IReadOnlyList<TodayExpenseOverview> Expenses);

public sealed class TodayOverviewService(ICategoryRepository categories, IExpenseRepository expenses)
{
    public async Task<Result<TodayOverview>> Get(DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var categoryTask = categories.GetAll(cancellationToken);
        var expenseTask = expenses.GetBetween(monthStart, monthEnd, cancellationToken);
        await Task.WhenAll(categoryTask, expenseTask);

        var categoryResult = await categoryTask;
        if (categoryResult.IsFailure)
            return Result<TodayOverview>.Failure(categoryResult.Error);

        var expenseResult = await expenseTask;
        if (expenseResult.IsFailure)
            return Result<TodayOverview>.Failure(expenseResult.Error);

        var activeCategories = categoryResult.Value
            .Where(category => !category.IsArchived)
            .OrderBy(category => category.Name)
            .ToList();
        var monthlyExpenses = expenseResult.Value;
        var categoryOverview = activeCategories
            .Select(category => new TodayCategoryOverview(
                category.CategoryId,
                category.Name,
                category.Icon,
                category.Color,
                monthlyExpenses
                    .Where(expense => expense.CategoryId == category.CategoryId)
                    .Sum(expense => expense.Amount.Cents),
                category.MonthlyBudget?.Cents))
            .ToList();
        var categoryLookup = categoryResult.Value.ToDictionary(category => category.CategoryId);
        var todayExpenses = monthlyExpenses
            .Where(expense => expense.OccurredOn == today)
            .Select(expense => ToOverview(expense, categoryLookup))
            .ToList();
        var configuredBudgets = activeCategories
            .Where(category => category.MonthlyBudget.HasValue)
            .Select(category => category.MonthlyBudget!.Value.Cents)
            .ToList();
        return Result<TodayOverview>.Success(new TodayOverview(
            monthlyExpenses.Sum(expense => expense.Amount.Cents),
            configuredBudgets.Count == 0 ? null : configuredBudgets.Sum(),
            categoryOverview,
            todayExpenses));
    }

    private static TodayExpenseOverview ToOverview(
        Expense expense,
        IReadOnlyDictionary<Guid, Category> categories)
    {
        if (!categories.TryGetValue(expense.CategoryId, out var category))
            return new TodayExpenseOverview(expense.ExpenseId, "Categoria non disponibile", "more",
                "#6F7975", expense.Amount.Cents, expense.Note);

        return new TodayExpenseOverview(expense.ExpenseId, category.Name, category.Icon,
            category.Color, expense.Amount.Cents, expense.Note);
    }
}
