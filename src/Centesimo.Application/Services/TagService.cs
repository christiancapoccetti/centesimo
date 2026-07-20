using Centesimo.Domain;

namespace Centesimo.Application;

public sealed class TagService(ICategoryRepository categories, ITagRepository tags)
{
    public async Task<Result<IReadOnlyList<Tag>>> GetActive(Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var result = await tags.GetByCategory(categoryId, cancellationToken);
        if (result.IsFailure)
            return Result<IReadOnlyList<Tag>>.Failure(result.Error);

        var activeTags = result.Value
            .Where(tag => !tag.IsArchived)
            .OrderBy(tag => tag.Name)
            .ToList();
        return Result<IReadOnlyList<Tag>>.Success(activeTags);
    }

    public async Task<Result<Tag>> Create(Guid categoryId, string name,
        CancellationToken cancellationToken = default)
    {
        var found = await categories.Get(categoryId, cancellationToken);
        if (found.IsFailure)
            return Result<Tag>.Failure(found.Error);

        if (found.Value is null)
            return Result<Tag>.Failure(ApplicationErrors.CategoryNotFound);

        if (found.Value.IsArchived)
            return Result<Tag>.Failure(ApplicationErrors.CategoryArchived);

        if (name.IsEmpty())
            return Result<Tag>.Failure(ApplicationErrors.InvalidName);

        var tag = new Tag(Guid.NewGuid(), categoryId, name);
        var saved = await tags.Add(tag, cancellationToken);
        return saved.IsFailure ? Result<Tag>.Failure(saved.Error) : Result<Tag>.Success(tag);
    }

    public async Task<Result> Archive(Guid tagId, CancellationToken cancellationToken = default)
    {
        var found = await tags.Get(tagId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.TagNotFound);

        found.Value.Archive();
        return await tags.Update(found.Value, cancellationToken);
    }
}
