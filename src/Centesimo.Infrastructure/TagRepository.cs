using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

public sealed class TagRepository(CentesimoDbContext dbContext) : ITagRepository
{
    public Task<Tag?> Get(Guid tagId, CancellationToken cancellationToken = default) =>
        dbContext.Tags
            .AsNoTracking()
            .SingleOrDefaultAsync(tag => tag.TagId == tagId, cancellationToken);

    public async Task<IReadOnlyList<Tag>> GetByCategory(Guid categoryId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Tags
            .AsNoTracking()
            .Where(tag => tag.CategoryId == categoryId)
            .OrderBy(tag => tag.Name)
            .ToListAsync(cancellationToken);

    public async Task Add(Tag tag, CancellationToken cancellationToken = default)
    {
        dbContext.Tags.Add(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Update(Tag tag, CancellationToken cancellationToken = default)
    {
        var tracked = await dbContext.Tags
            .SingleOrDefaultAsync(item => item.TagId == tag.TagId, cancellationToken);

        if (tracked is null || !tag.IsArchived)
        {
            return;
        }

        tracked.Archive();
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
