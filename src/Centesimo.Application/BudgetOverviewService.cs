namespace Centesimo.Application;

public enum BudgetStatus
{
    NoBudget,
    OnTrack,
    Warning,
    Exceeded
}

public sealed record CategoryBudgetOverview(
    Guid CategoryId,
    string Name,
    long SpentCents,
    long? BudgetCents,
    decimal? PercentageUsed,
    BudgetStatus Status);

public sealed class BudgetOverviewService(
    ICategoryRepository categories,
    IExpenseRepository expenses)
{
    public async Task<Result<IReadOnlyList<CategoryBudgetOverview>>> GetMonth(int year, int month,
        CancellationToken cancellationToken = default)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1).AddDays(-1);
        var categoryTask = categories.GetAll(cancellationToken);
        var expenseTask = expenses.GetBetween(from, to, cancellationToken);
        await Task.WhenAll(categoryTask, expenseTask);
        var categoryResult = await categoryTask;
        if (categoryResult.IsFailure)
            return Result<IReadOnlyList<CategoryBudgetOverview>>.Failure(categoryResult.Error);

        var expenseResult = await expenseTask;
        if (expenseResult.IsFailure)
            return Result<IReadOnlyList<CategoryBudgetOverview>>.Failure(expenseResult.Error);

        var monthlyExpenses = expenseResult.Value;
        var overview = categoryResult.Value
            .Select(category =>
            {
                var spent = monthlyExpenses
                    .Where(expense => expense.CategoryId == category.CategoryId)
                    .Sum(expense => expense.Amount.Cents);
                var budget = category.MonthlyBudget?.Cents;
                decimal? percentage = budget > 0
                    ? decimal.Round(spent * 100m / budget.Value, 1)
                    : null;
                var status = percentage switch
                {
                    null => BudgetStatus.NoBudget,
                    >= 100 => BudgetStatus.Exceeded,
                    >= 80 => BudgetStatus.Warning,
                    _ => BudgetStatus.OnTrack
                };
                return new CategoryBudgetOverview(
                    category.CategoryId,
                    category.Name,
                    spent,
                    budget,
                    percentage,
                    status);
            })
            .ToList();
        return Result<IReadOnlyList<CategoryBudgetOverview>>.Success(overview);
    }
}


