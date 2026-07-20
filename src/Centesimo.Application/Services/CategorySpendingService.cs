using Centesimo.Domain;

namespace Centesimo.Application;

public sealed record CategoryExpenseOverview(Guid ExpenseId, long AmountCents, DateOnly OccurredOn, string Note);

public sealed record TagSpendingOverview(Guid? TagId, string Name, long SpentCents,
    IReadOnlyList<CategoryExpenseOverview> Expenses);

public sealed record CategorySpendingOverview(Guid CategoryId, string CategoryName, string CategoryIcon,
    string CategoryColor, int Year, int Month, long SpentCents, long? BudgetCents,
    IReadOnlyList<TagSpendingOverview> Tags);

public sealed class CategorySpendingService(ICategoryRepository categories, ITagRepository tags,
    IExpenseRepository expenses)
{
    public async Task<Result<CategorySpendingOverview>> Get(Guid categoryId, int year, int month,
        CancellationToken cancellationToken = default)
    {
        var categoryResult = await categories.Get(categoryId, cancellationToken);
        if (categoryResult.IsFailure)
            return Result<CategorySpendingOverview>.Failure(categoryResult.Error);

        if (categoryResult.Value is null)
            return Result<CategorySpendingOverview>.Failure(ApplicationErrors.CategoryNotFound);

        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var tagResult = await tags.GetByCategory(categoryId, cancellationToken);
        if (tagResult.IsFailure)
            return Result<CategorySpendingOverview>.Failure(tagResult.Error);

        var expenseResult = await expenses.GetByCategoryBetween(categoryId, monthStart, monthEnd, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<CategorySpendingOverview>.Failure(expenseResult.Error);

        var categoryExpenses = expenseResult.Value
            .OrderByDescending(expense => expense.OccurredOn)
            .ThenByDescending(expense => expense.ExpenseId)
            .ToList();
        var tagNames = tagResult.Value.ToDictionary(tag => tag.TagId, tag => tag.Name);
        var spendingByTag = categoryExpenses
            .GroupBy(expense => expense.TagId)
            .Select(group => new TagSpendingOverview(
                group.Key,
                group.Key.HasValue && tagNames.TryGetValue(group.Key.Value, out var name) ? name : "Senza tag",
                group.Sum(expense => expense.Amount.Cents),
                group.Select(ToOverview).ToList()))
            .OrderByDescending(group => group.SpentCents)
            .ThenBy(group => group.Name)
            .ToList();
        var category = categoryResult.Value;
        return Result<CategorySpendingOverview>.Success(new CategorySpendingOverview(
            category.CategoryId,
            category.Name,
            category.Icon,
            category.Color,
            year,
            month,
            categoryExpenses.Sum(expense => expense.Amount.Cents),
            category.MonthlyBudget?.Cents,
            spendingByTag));
    }

    private static CategoryExpenseOverview ToOverview(Expense expense) => new(
        expense.ExpenseId,
        expense.Amount.Cents,
        expense.OccurredOn,
        expense.Note);
}
