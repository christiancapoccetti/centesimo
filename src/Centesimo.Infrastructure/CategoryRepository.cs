using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

public sealed class CategoryRepository(CentesimoDbContext context)
    : RepositoryBase(context), ICategoryRepository
{
    public Task<Result<Category?>> Get(Guid categoryId, CancellationToken cancellationToken = default) =>
        UseContext((db, token) => db.Categories.AsNoTracking()
            .SingleOrDefaultAsync(category => category.CategoryId == categoryId, token), cancellationToken);

    public Task<Result<IReadOnlyList<Category>>> GetAll(CancellationToken cancellationToken = default) =>
        UseContext<IReadOnlyList<Category>>(async (db, token) => await db.Categories.AsNoTracking()
            .OrderBy(category => category.Name).ToListAsync(token), cancellationToken);

    public Task<Result> Add(Category category, CancellationToken cancellationToken = default) =>
        SaveContext(db => db.Categories.Add(category), cancellationToken);

    public Task<Result> Update(Category category, CancellationToken cancellationToken = default) =>
        SaveContext(async (db, token) =>
        {
            var tracked = await db.Categories.SingleOrDefaultAsync(
                item => item.CategoryId == category.CategoryId, token);
            if (tracked is null)
                return;

            tracked.UpdateDetails(category.Name, category.Icon, category.Color, category.MonthlyBudget);
            if (category.IsArchived)
                tracked.Archive();
        }, cancellationToken);
}
