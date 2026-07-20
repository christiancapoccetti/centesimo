using Centesimo.Domain;

namespace Centesimo.Application;

public sealed record ExpenseHistoryItem(
    Guid ExpenseId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    long AmountCents,
    DateOnly OccurredOn,
    string Note);

public sealed class ExpenseHistoryService(
    ICategoryRepository categories,
    IExpenseRepository expenses)
{
    public async Task<Result<IReadOnlyList<ExpenseHistoryItem>>> GetMonth(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var categoryTask = categories.GetAll(cancellationToken);
        var expenseTask = expenses.GetBetween(from, to, cancellationToken);
        await Task.WhenAll(categoryTask, expenseTask);

        var categoryResult = await categoryTask;
        if (categoryResult.IsFailure)
            return Result<IReadOnlyList<ExpenseHistoryItem>>.Failure(categoryResult.Error);

        var expenseResult = await expenseTask;
        if (expenseResult.IsFailure)
            return Result<IReadOnlyList<ExpenseHistoryItem>>.Failure(expenseResult.Error);

        var categoryLookup = categoryResult.Value.ToDictionary(category => category.CategoryId);
        var items = expenseResult.Value
            .OrderByDescending(expense => expense.OccurredOn)
            .Select(expense => ToItem(expense, categoryLookup))
            .ToList();
        return Result<IReadOnlyList<ExpenseHistoryItem>>.Success(items);
    }

    private static ExpenseHistoryItem ToItem(
        Expense expense,
        IReadOnlyDictionary<Guid, Category> categories)
    {
        if (!categories.TryGetValue(expense.CategoryId, out var category))
            return new ExpenseHistoryItem(
                expense.ExpenseId,
                "Categoria non disponibile",
                "more",
                "#6F7975",
                expense.Amount.Cents,
                expense.OccurredOn,
                expense.Note);

        return new ExpenseHistoryItem(
            expense.ExpenseId,
            category.Name,
            category.Icon,
            category.Color,
            expense.Amount.Cents,
            expense.OccurredOn,
            expense.Note);
    }
}
