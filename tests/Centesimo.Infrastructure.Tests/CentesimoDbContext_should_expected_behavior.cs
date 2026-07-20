using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure.Tests;

public sealed class CentesimoDbContext_should_expected_behavior
{
    [Fact]
    public async Task Persist_money_as_integer_cents()
    {
        using var database = new TestDatabase();
        var category = CreateCategory();
        database.Context.Categories.Add(category);
        database.Context.Expenses.Add(new Expense(Guid.NewGuid(), category.CategoryId,
            new Money(1234), new DateOnly(2026, 7, 20)));
        await database.Context.SaveChangesAsync();

        var stored = await database.Context.Database
            .SqlQuery<long>($"SELECT Amount AS Value FROM Expenses")
            .SingleAsync();

        Assert.Equal(1234, stored);
    }

    [Fact]
    public async Task Enforce_unique_tag_name_within_category()
    {
        using var database = new TestDatabase();
        var category = CreateCategory();
        database.Context.Add(category);
        database.Context.AddRange(
            new Tag(Guid.NewGuid(), category.CategoryId, "Market"),
            new Tag(Guid.NewGuid(), category.CategoryId, "Market"));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Enforce_unique_recurrence_occurrence()
    {
        using var database = new TestDatabase();
        var paymentId = Guid.NewGuid();
        var dueOn = new DateOnly(2026, 7, 20);
        database.Context.Add(new RecurrenceOccurrence(paymentId, dueOn));
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        database.Context.Add(new RecurrenceOccurrence(paymentId, dueOn));

        await Assert.ThrowsAsync<DbUpdateException>(() => database.Context.SaveChangesAsync());
    }

    private static Category CreateCategory() =>
        new(Guid.NewGuid(), "Groceries", "cart", "#123456");
}

