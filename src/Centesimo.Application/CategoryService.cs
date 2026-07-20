using Centesimo.Domain;

namespace Centesimo.Application;

public sealed class CategoryService(ICategoryRepository categories)
{
    public async Task<Result<Category>> Create(string name, string icon, string color,
        Money? budget = null, CancellationToken cancellationToken = default)
    {
        if (name.IsEmpty())
            return Result<Category>.Failure(ApplicationErrors.InvalidName);

        var category = new Category(Guid.NewGuid(), name, icon, color, budget);
        var saved = await categories.Add(category, cancellationToken);
        return saved.IsFailure
            ? Result<Category>.Failure(saved.Error)
            : Result<Category>.Success(category);
    }

    public async Task<Result> SetBudget(Guid categoryId, Money? budget,
        CancellationToken cancellationToken = default)
    {
        var found = await categories.Get(categoryId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.CategoryNotFound);

        found.Value.SetBudget(budget);
        return await categories.Update(found.Value, cancellationToken);
    }

    public async Task<Result> Archive(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var found = await categories.Get(categoryId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.CategoryNotFound);

        found.Value.Archive();
        return await categories.Update(found.Value, cancellationToken);
    }
}
