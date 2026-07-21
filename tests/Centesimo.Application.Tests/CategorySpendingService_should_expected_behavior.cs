using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class CategorySpendingService_should_expected_behavior
{
    [Fact]
    public async Task Should_group_category_expenses_by_tag_and_order_by_total()
    {
        var categories = new FakeCategoryRepository();
        var tags = new FakeTagRepository();
        var expenses = new FakeExpenseRepository();
        var food = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B", new Money(2000));
        var market = new Tag(Guid.NewGuid(), food.CategoryId, "Market");
        categories.Items.Add(food);
        tags.Items.Add(market);
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(900), new DateOnly(2026, 7, 10), market.TagId),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(700), new DateOnly(2026, 7, 12), market.TagId),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(500), new DateOnly(2026, 7, 11)),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(5000), new DateOnly(2026, 6, 30))
        ]);
        var service = new CategorySpendingService(categories, tags, expenses);

        var result = await service.Get(food.CategoryId, 2026, 7);

        Assert.True(result.IsSuccess);
        Assert.Equal(2100, result.Value.SpentCents);
        Assert.Equal(2000, result.Value.BudgetCents);
        Assert.Collection(result.Value.Tags,
            tag =>
            {
                Assert.Equal("Market", tag.Name);
                Assert.Equal(1600, tag.SpentCents);
                Assert.Equal(2, tag.Expenses.Count);
                Assert.Equal(new DateOnly(2026, 7, 12), tag.Expenses[0].OccurredOn);
            },
            tag =>
            {
                Assert.Equal("Senza tag", tag.Name);
                Assert.Equal(500, tag.SpentCents);
                Assert.Single(tag.Expenses);
            });
    }

    [Fact]
    public async Task Should_return_not_found_when_category_does_not_exist()
    {
        var service = new CategorySpendingService(
            new FakeCategoryRepository(),
            new FakeTagRepository(),
            new FakeExpenseRepository());

        var result = await service.Get(Guid.NewGuid(), 2026, 7);

        Assert.True(result.IsFailure);
        Assert.Equal(ApplicationErrors.CategoryNotFound, result.Error);
    }

    [Fact]
    public async Task Should_aggregate_the_full_year_and_annualize_the_budget()
    {
        var categories = new FakeCategoryRepository();
        var tags = new FakeTagRepository();
        var expenses = new FakeExpenseRepository();
        var food = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B", new Money(2000));
        var market = new Tag(Guid.NewGuid(), food.CategoryId, "Market");
        categories.Items.Add(food);
        tags.Items.Add(market);
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(900), new DateOnly(2026, 1, 10), market.TagId),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(700), new DateOnly(2026, 12, 12), market.TagId),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(500), new DateOnly(2025, 12, 31), market.TagId),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(600), new DateOnly(2027, 1, 1), market.TagId)
        ]);

        var result = await new CategorySpendingService(categories, tags, expenses)
            .Get(food.CategoryId, 2026, 7, CategorySpendingPeriod.Year);

        Assert.True(result.IsSuccess);
        Assert.Equal(CategorySpendingPeriod.Year, result.Value.Period);
        Assert.Equal(1600, result.Value.SpentCents);
        Assert.Equal(24000, result.Value.BudgetCents);
        var tag = Assert.Single(result.Value.Tags);
        Assert.Equal("Market", tag.Name);
        Assert.Equal(2, tag.Expenses.Count);
    }
}
