using Centesimo.Domain;

namespace Centesimo.Application;

public sealed class CategoryService(ICategoryRepository categories)
{
    public async Task<Result<IReadOnlyList<Category>>> GetActive(
        CancellationToken cancellationToken = default)
    {
        var result = await categories.GetAll(cancellationToken);
        if (result.IsFailure)
            return Result<IReadOnlyList<Category>>.Failure(result.Error);

        var active = result.Value
            .Where(category => !category.IsArchived)
            .OrderBy(category => category.Name)
            .ToList();
        return Result<IReadOnlyList<Category>>.Success(active);
    }
    public async Task<Result<Category>> Create(string name, string icon, string color,
        Money? budget = null, CancellationToken cancellationToken = default)
    {
        if (name.IsEmpty())
            return Result<Category>.Failure(ApplicationErrors.InvalidName);

        var availability = await EnsureNameAvailable(name, null, cancellationToken);
        if (availability.IsFailure)
            return Result<Category>.Failure(availability.Error);

        var category = new Category(Guid.NewGuid(), name, icon, color, budget);
        var saved = await categories.Add(category, cancellationToken);
        return saved.IsFailure
            ? Result<Category>.Failure(saved.Error)
            : Result<Category>.Success(category);
    }

    public async Task<Result> Update(Guid categoryId, string name, string icon, string color,
        Money? budget, CancellationToken cancellationToken = default)
    {
        if (name.IsEmpty())
            return Result.Failure(ApplicationErrors.InvalidName);

        var found = await categories.Get(categoryId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.CategoryNotFound);

        var availability = await EnsureNameAvailable(name, categoryId, cancellationToken);
        if (availability.IsFailure)
            return availability;

        found.Value.UpdateDetails(name, icon, color, budget);
        return await categories.Update(found.Value, cancellationToken);
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
    private async Task<Result> EnsureNameAvailable(string name, Guid? excludedCategoryId,
        CancellationToken cancellationToken)
    {
        var result = await categories.GetAll(cancellationToken);
        if (result.IsFailure)
            return Result.Failure(result.Error);

        var normalizedName = name.Trim();
        var duplicateExists = result.Value.Any(category =>
            category.CategoryId != excludedCategoryId &&
            category.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));
        return duplicateExists
            ? Result.Failure(ApplicationErrors.CategoryNameAlreadyExists)
            : Result.Success();
    }}
