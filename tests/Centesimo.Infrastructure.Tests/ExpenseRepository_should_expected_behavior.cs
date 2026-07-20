using Centesimo.Domain;

namespace Centesimo.Infrastructure.Tests;

public sealed class ExpenseRepository_should_expected_behavior
{
    [Fact]
    public async Task Query_by_inclusive_date_range_and_delete()
    {
        using var database = new TestDatabase();
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#123456");
        database.Context.Add(category);
        await database.Context.SaveChangesAsync();
        var repository = new ExpenseRepository(database.Context);
        var included = new Expense(Guid.NewGuid(), category.CategoryId, new Money(100),
            new DateOnly(2026, 7, 20));
        var excluded = new Expense(Guid.NewGuid(), category.CategoryId, new Money(200),
            new DateOnly(2026, 6, 30));
        await repository.Add(included);
        await repository.Add(excluded);
        database.Context.ChangeTracker.Clear();

        var expenses = await repository.GetBetween(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));
        await repository.Delete(included.ExpenseId);

        Assert.Single(expenses.Value);
        Assert.Equal(included.ExpenseId, expenses.Value[0].ExpenseId);
        Assert.Null((await repository.Get(included.ExpenseId)).Value);
    }
}
