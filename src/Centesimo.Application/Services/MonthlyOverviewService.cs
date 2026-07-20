using Centesimo.Domain;

namespace Centesimo.Application;

public sealed record MonthlyCategoryOverview(Guid CategoryId, string Name, string Icon, string Color,
    long SpentCents, long? BudgetCents);

public sealed record MonthlyExpenseOverview(Guid ExpenseId, string CategoryName, string CategoryIcon,
    string CategoryColor, long AmountCents, DateOnly OccurredOn, string Note);

public sealed record MonthlyOverview(long SpentCents, long? TotalBudgetCents,
    IReadOnlyList<MonthlyCategoryOverview> Categories, IReadOnlyList<MonthlyExpenseOverview> Expenses);

public sealed class MonthlyOverviewService(ICategoryRepository categories, IExpenseRepository expenses)
{
    public async Task<Result<MonthlyOverview>> Get(int year, int month,
        CancellationToken cancellationToken = default)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<MonthlyOverview>.Failure(categoryResult.Error);

        var expenseResult = await expenses.GetBetween(monthStart, monthEnd, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<MonthlyOverview>.Failure(expenseResult.Error);

        var activeCategories = categoryResult.Value
            .Where(category => !category.IsArchived)
            .OrderBy(category => category.Name)
            .ToList();
        var monthlyExpenses = expenseResult.Value;
        var spendingByCategory = monthlyExpenses
            .GroupBy(expense => expense.CategoryId)
            .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount.Cents));
        var categoryOverview = activeCategories
            .Select(category => new MonthlyCategoryOverview(
                category.CategoryId,
                category.Name,
                category.Icon,
                category.Color,
                spendingByCategory.GetValueOrDefault(category.CategoryId),
                category.MonthlyBudget?.Cents))
            .Where(category => category.SpentCents > 0)
            .OrderByDescending(category => category.SpentCents)
            .ThenBy(category => category.Name)
            .ToList();
        var categoryLookup = categoryResult.Value.ToDictionary(category => category.CategoryId);
        var expenseOverview = monthlyExpenses
            .OrderByDescending(expense => expense.OccurredOn)
            .ThenByDescending(expense => expense.ExpenseId)
            .Take(5)
            .Select(expense => ToOverview(expense, categoryLookup))
            .ToList();
        var configuredBudgets = activeCategories
            .Where(category => category.MonthlyBudget.HasValue)
            .Select(category => category.MonthlyBudget!.Value.Cents)
            .ToList();
        return Result<MonthlyOverview>.Success(new MonthlyOverview(
            monthlyExpenses.Sum(expense => expense.Amount.Cents),
            configuredBudgets.Count == 0 ? null : configuredBudgets.Sum(),
            categoryOverview,
            expenseOverview));
    }

    private static MonthlyExpenseOverview ToOverview(
        Expense expense,
        IReadOnlyDictionary<Guid, Category> categories)
    {
        if (!categories.TryGetValue(expense.CategoryId, out var category))
            return new MonthlyExpenseOverview(expense.ExpenseId, "Categoria non disponibile", "more",
                "#6F7975", expense.Amount.Cents, expense.OccurredOn, expense.Note);

        return new MonthlyExpenseOverview(expense.ExpenseId, category.Name, category.Icon,
            category.Color, expense.Amount.Cents, expense.OccurredOn, expense.Note);
    }
}