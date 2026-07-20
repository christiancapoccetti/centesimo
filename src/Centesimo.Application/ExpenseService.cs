using Centesimo.Domain;

namespace Centesimo.Application;

public sealed record SaveExpenseRequest(Guid CategoryId, long AmountCents, DateOnly OccurredOn,
    Guid? TagId = null, string Note = "", string? PhotoPath = null);

public sealed class ExpenseService(
    ICategoryRepository categories,
    ITagRepository tags,
    IExpenseRepository expenses)
{
    public async Task<Result<Expense>> Create(SaveExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateForRecurringPayment(request, cancellationToken);
        if (validation.IsFailure)
            return Result<Expense>.Failure(validation.Error);

        var expense = request.ToExpense(Guid.NewGuid());
        var saved = await expenses.Add(expense, cancellationToken);
        return saved.IsFailure ? Result<Expense>.Failure(saved.Error) : Result<Expense>.Success(expense);
    }

    public async Task<Result> Update(Guid expenseId, SaveExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var found = await expenses.Get(expenseId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.ExpenseNotFound);

        var validation = await ValidateForRecurringPayment(request, cancellationToken);
        if (validation.IsFailure)
            return validation;

        return await expenses.Update(request.ToExpense(expenseId), cancellationToken);
    }

    public async Task<Result> Delete(Guid expenseId, CancellationToken cancellationToken = default)
    {
        var found = await expenses.Get(expenseId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.ExpenseNotFound);

        return await expenses.Delete(expenseId, cancellationToken);
    }

    internal async Task<Result> ValidateForRecurringPayment(SaveExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AmountCents <= 0)
            return Result.Failure(ApplicationErrors.InvalidAmount);

        var categoryResult = await categories.Get(request.CategoryId, cancellationToken);
        if (categoryResult.IsFailure)
            return Result.Failure(categoryResult.Error);

        if (categoryResult.Value is null)
            return Result.Failure(ApplicationErrors.CategoryNotFound);

        if (categoryResult.Value.IsArchived)
            return Result.Failure(ApplicationErrors.CategoryArchived);

        if (!request.TagId.HasValue)
            return Result.Success();

        var tagResult = await tags.Get(request.TagId.Value, cancellationToken);
        if (tagResult.IsFailure)
            return Result.Failure(tagResult.Error);

        if (tagResult.Value is null)
            return Result.Failure(ApplicationErrors.TagNotFound);

        if (tagResult.Value.IsArchived)
            return Result.Failure(ApplicationErrors.TagArchived);

        return tagResult.Value.CategoryId == request.CategoryId
            ? Result.Success()
            : Result.Failure(ApplicationErrors.TagCategoryMismatch);
    }
}
