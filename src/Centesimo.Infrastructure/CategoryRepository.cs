using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

public sealed class CategoryRepository(CentesimoDbContext dbContext) : ICategoryRepository
{
    public Task<Category?> Get(Guid categoryId, CancellationToken cancellationToken = default) =>
        dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.CategoryId == categoryId, cancellationToken);

    public async Task<IReadOnlyList<Category>> GetAll(CancellationToken cancellationToken = default) =>
        await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync(cancellationToken);

    public async Task Add(Category category, CancellationToken cancellationToken = default)
    {
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Update(Category category, CancellationToken cancellationToken = default)
    {
        var tracked = await dbContext.Categories
            .SingleOrDefaultAsync(item => item.CategoryId == category.CategoryId, cancellationToken);

        if (tracked is null)
        {
            return;
        }

        tracked.SetBudget(category.MonthlyBudget);
        if (category.IsArchived)
        {
            tracked.Archive();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
