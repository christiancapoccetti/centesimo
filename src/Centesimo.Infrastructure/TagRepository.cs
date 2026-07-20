using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

public sealed class TagRepository(CentesimoDbContext context)
    : RepositoryBase(context), ITagRepository
{
    public Task<Result<Tag?>> Get(Guid tagId, CancellationToken cancellationToken = default) =>
        UseContext((db, token) => db.Tags.AsNoTracking()
            .SingleOrDefaultAsync(tag => tag.TagId == tagId, token), cancellationToken);

    public Task<Result<IReadOnlyList<Tag>>> GetByCategory(Guid categoryId,
        CancellationToken cancellationToken = default) =>
        UseContext<IReadOnlyList<Tag>>(async (db, token) => await db.Tags.AsNoTracking()
            .Where(tag => tag.CategoryId == categoryId).OrderBy(tag => tag.Name)
            .ToListAsync(token), cancellationToken);

    public Task<Result> Add(Tag tag, CancellationToken cancellationToken = default) =>
        SaveContext(db => db.Tags.Add(tag), cancellationToken);

    public Task<Result> Update(Tag tag, CancellationToken cancellationToken = default) =>
        SaveContext(async (db, token) =>
        {
            var tracked = await db.Tags.SingleOrDefaultAsync(item => item.TagId == tag.TagId, token);
            if (tracked is null || !tag.IsArchived)
                return;

            tracked.Archive();
        }, cancellationToken);
}
