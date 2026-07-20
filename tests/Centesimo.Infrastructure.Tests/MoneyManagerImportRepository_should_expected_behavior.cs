using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure.Tests;

public sealed class MoneyManagerImportRepository_should_expected_behavior
{
    [Fact]
    public async Task Import_is_idempotent()
    {
        using var database = new TestDatabase();
        var repository = new MoneyManagerImportRepository(database.Context);
        var data = CreateData();

        var first = await repository.Import(data);
        database.Context.ChangeTracker.Clear();
        var second = await repository.Import(data);

        Assert.True(first.IsSuccess);
        Assert.Equal(new MoneyManagerPersisted(1, 1, 1), first.Value);
        Assert.True(second.IsSuccess);
        Assert.Equal(new MoneyManagerPersisted(0, 0, 0), second.Value);
        Assert.Equal(1, await database.Context.Categories.CountAsync());
        Assert.Equal(1, await database.Context.Tags.CountAsync());
        Assert.Equal(1, await database.Context.Expenses.CountAsync());
    }

    [Fact]
    public async Task Import_reuses_an_existing_category_with_the_same_name()
    {
        using var database = new TestDatabase();
        var categoryId = Guid.NewGuid();
        database.Context.Categories.Add(new Category(categoryId, "Food", "cart", "#176B5B"));
        await database.Context.SaveChangesAsync();
        var repository = new MoneyManagerImportRepository(database.Context);

        var result = await repository.Import(CreateData());

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.CategoriesAdded);
        Assert.Equal(categoryId, (await database.Context.Expenses.SingleAsync()).CategoryId);
        Assert.Equal(categoryId, (await database.Context.Tags.SingleAsync()).CategoryId);
    }

    [Fact]
    public async Task Import_ignores_duplicate_source_expenses()
    {
        using var database = new TestDatabase();
        var repository = new MoneyManagerImportRepository(database.Context);
        var original = CreateData();
        var data = original with { Expenses = [original.Expenses[0], original.Expenses[0]] };

        var result = await repository.Import(data);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.ExpensesAdded);
        Assert.Equal(1, await database.Context.Expenses.CountAsync());
    }
    [Fact]
    public async Task Import_restores_matching_archived_categories_and_tags()
    {
        using var database = new TestDatabase();
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B");
        category.Archive();
        var tag = new Tag(Guid.NewGuid(), category.CategoryId, "Restaurant");
        tag.Archive();
        database.Context.AddRange(category, tag);
        await database.Context.SaveChangesAsync();
        var repository = new MoneyManagerImportRepository(database.Context);

        var result = await repository.Import(CreateData());

        Assert.True(result.IsSuccess);
        Assert.False((await database.Context.Categories.SingleAsync()).IsArchived);
        Assert.False((await database.Context.Tags.SingleAsync()).IsArchived);
        Assert.Equal(category.CategoryId, (await database.Context.Expenses.SingleAsync()).CategoryId);
        Assert.Equal(tag.TagId, (await database.Context.Expenses.SingleAsync()).TagId);
    }
    private static MoneyManagerImportData CreateData() => new(
        [new MoneyManagerCategory("category-1", "Food", "cart", "#176B5B")],
        [new MoneyManagerTag("tag-1:category-1", "category-1", "Restaurant")],
        [new MoneyManagerExpense("expense-1", "category-1", "tag-1:category-1", 1234,
            new DateOnly(2026, 7, 20), "Lunch")],
        0,
        0);
}