using Centesimo.Domain;

namespace Centesimo.Infrastructure.Tests;

public sealed class ExpenseRepository_should_expected_behavior
{
    [Fact]
    public async Task Query_by_inclusive_date_range_and_delete()
    {
        using var database = new TestDatabase();
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#123456");
        var otherCategory = new Category(Guid.NewGuid(), "Travel", "car", "#4F5F7A");
        database.Context.AddRange(category, otherCategory);
        await database.Context.SaveChangesAsync();
        var repository = new ExpenseRepository(database.Context);
        var included = new Expense(Guid.NewGuid(), category.CategoryId, new Money(100),
            new DateOnly(2026, 7, 20));
        var excluded = new Expense(Guid.NewGuid(), category.CategoryId, new Money(200),
            new DateOnly(2026, 6, 30));
        await repository.Add(included);
        await repository.Add(excluded);
        await repository.Add(new Expense(Guid.NewGuid(), otherCategory.CategoryId, new Money(300),
            new DateOnly(2026, 7, 20)));
        database.Context.ChangeTracker.Clear();

        var expenses = await repository.GetBetween(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));
        var categoryExpenses = await repository.GetByCategoryBetween(category.CategoryId,
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));
        await repository.Delete(included.ExpenseId);

        Assert.Equal(2, expenses.Value.Count);
        Assert.Single(categoryExpenses.Value);
        Assert.Equal(included.ExpenseId, categoryExpenses.Value[0].ExpenseId);
        Assert.Null((await repository.Get(included.ExpenseId)).Value);
    }
}
