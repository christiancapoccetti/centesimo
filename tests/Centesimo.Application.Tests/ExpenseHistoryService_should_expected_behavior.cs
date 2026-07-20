using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class ExpenseHistoryService_should_expected_behavior
{
    [Fact]
    public async Task Should_return_month_expenses_newest_first_with_category_details()
    {
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B");
        var categories = new FakeCategoryRepository();
        categories.Items.Add(category);
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(500), new DateOnly(2026, 7, 2)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(800), new DateOnly(2026, 7, 20), note: "Dinner"),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(300), new DateOnly(2026, 6, 30))
        ]);
        var service = new ExpenseHistoryService(categories, expenses);

        var result = await service.GetMonth(2026, 7);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value,
            item =>
            {
                Assert.Equal(new DateOnly(2026, 7, 20), item.OccurredOn);
                Assert.Equal("Food", item.CategoryName);
                Assert.Equal("Dinner", item.Note);
            },
            item => Assert.Equal(new DateOnly(2026, 7, 2), item.OccurredOn));
    }
}
