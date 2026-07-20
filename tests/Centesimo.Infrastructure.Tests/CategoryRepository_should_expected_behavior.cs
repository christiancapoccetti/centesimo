using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure.Tests;

public sealed class CategoryRepository_should_expected_behavior
{
    [Fact]
    public async Task Add_and_read_without_tracking()
    {
        using var database = new TestDatabase();
        var repository = new CategoryRepository(database.Context);
        var category = new Category(Guid.NewGuid(), "Groceries", "cart", "#123456");

        await repository.Add(category);
        database.Context.ChangeTracker.Clear();
        var loaded = await repository.Get(category.CategoryId);

        Assert.NotNull(loaded);
        Assert.Empty(database.Context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Update_a_tracked_entity_loaded_by_repository()
    {
        using var database = new TestDatabase();
        var repository = new CategoryRepository(database.Context);
        var category = new Category(Guid.NewGuid(), "Groceries", "cart", "#123456");
        await repository.Add(category);
        database.Context.ChangeTracker.Clear();
        category.SetBudget(new Money(30_000));
        category.Archive();

        await repository.Update(category);
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.Categories.AsNoTracking().SingleAsync();

        Assert.Equal(30_000, stored.MonthlyBudget?.Cents);
        Assert.True(stored.IsArchived);
    }
}
