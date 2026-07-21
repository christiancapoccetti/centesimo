using Centesimo.Domain;

namespace Centesimo.Application;

public sealed record YearlyCategoryOverview(
    Guid CategoryId,
    string Name,
    string Icon,
    string Color,
    long SpentCents,
    long? BudgetCents);

public sealed record YearlyOverview(
    long SpentCents,
    long? TotalBudgetCents,
    IReadOnlyList<YearlyCategoryOverview> Categories,
    IReadOnlyList<YearlyExpenseOverview> Expenses);

public sealed record YearlyExpenseOverview(
    Guid ExpenseId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    long AmountCents,
    DateOnly OccurredOn,
    string Note);

public sealed class YearlyOverviewService(ICategoryRepository categories, IExpenseRepository expenses)
{
    public async Task<Result<YearlyOverview>> Get(int year, CancellationToken cancellationToken = default)
    {
        var from = new DateOnly(year, 1, 1);
        var to = new DateOnly(year, 12, 31);
        var categoryTask = categories.GetAll(cancellationToken);
        var expenseTask = expenses.GetBetween(from, to, cancellationToken);
        await Task.WhenAll(categoryTask, expenseTask);
        var categoryResult = await categoryTask;
        if (categoryResult.IsFailure)
            return Result<YearlyOverview>.Failure(categoryResult.Error);

        var expenseResult = await expenseTask;
        if (expenseResult.IsFailure)
            return Result<YearlyOverview>.Failure(expenseResult.Error);

        var yearlyExpenses = expenseResult.Value;
        var spendingByCategory = yearlyExpenses
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount.Cents));
        var activeCategories = categoryResult.Value.Where(category => !category.IsArchived).ToList();
        var categoryOverview = activeCategories
            .Select(category => new YearlyCategoryOverview(
                category.CategoryId,
                category.Name,
                category.Icon,
                category.Color,
                spendingByCategory.GetValueOrDefault(category.CategoryId),
                category.MonthlyBudget.HasValue ? category.MonthlyBudget.Value.Cents * 12 : null))
            .Where(category => category.SpentCents > 0)
            .OrderByDescending(category => category.SpentCents)
            .ThenBy(category => category.Name)
            .ToList();
        var configuredBudgets = activeCategories
            .Where(category => category.MonthlyBudget.HasValue)
            .Select(category => category.MonthlyBudget!.Value.Cents * 12)
            .ToList();
        var categoryLookup = categoryResult.Value.ToDictionary(category => category.CategoryId);
        var expenseOverview = yearlyExpenses
            .OrderByDescending(expense => expense.Amount.Cents)
            .ThenByDescending(expense => expense.OccurredOn)
            .ThenByDescending(expense => expense.ExpenseId)
            .Take(10)
            .Select(expense => ToOverview(expense, categoryLookup))
            .ToList();
        return Result<YearlyOverview>.Success(new YearlyOverview(
            yearlyExpenses.Sum(expense => expense.Amount.Cents),
            configuredBudgets.Count == 0 ? null : configuredBudgets.Sum(),
            categoryOverview,
            expenseOverview));
    }

    private static YearlyExpenseOverview ToOverview(
        Expense expense,
        IReadOnlyDictionary<Guid, Category> categories)
    {
        if (!categories.TryGetValue(expense.CategoryId, out var category))
            return new YearlyExpenseOverview(expense.ExpenseId, "Categoria non disponibile", "more",
                "#6F7975", expense.Amount.Cents, expense.OccurredOn, expense.Note);

        return new YearlyExpenseOverview(expense.ExpenseId, category.Name, category.Icon,
            category.Color, expense.Amount.Cents, expense.OccurredOn, expense.Note);
    }
}
