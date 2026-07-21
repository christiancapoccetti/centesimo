using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class YearlyOverviewService_should_expected_behavior
{
    [Fact]
    public async Task Should_aggregate_all_expenses_and_annualize_active_category_budgets()
    {
        var categories = new FakeCategoryRepository();
        var expenses = new FakeExpenseRepository();
        var food = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B", new Money(20000));
        var travel = new Category(Guid.NewGuid(), "Travel", "car", "#4F5F7A");
        var archived = new Category(Guid.NewGuid(), "Old", "more", "#6F7975", new Money(5000));
        archived.Archive();
        categories.Items.AddRange([food, travel, archived]);
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(1500), new DateOnly(2026, 1, 1)),
            new Expense(Guid.NewGuid(), travel.CategoryId, new Money(3000), new DateOnly(2026, 12, 31)),
            new Expense(Guid.NewGuid(), archived.CategoryId, new Money(500), new DateOnly(2026, 6, 1)),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(900), new DateOnly(2025, 12, 31))
        ]);

        var result = await new YearlyOverviewService(categories, expenses).Get(2026);

        Assert.True(result.IsSuccess);
        Assert.Equal(5000, result.Value.SpentCents);
        Assert.Equal(240000, result.Value.TotalBudgetCents);
        Assert.Collection(result.Value.Categories,
            category => Assert.Equal(travel.CategoryId, category.CategoryId),
            category => Assert.Equal(food.CategoryId, category.CategoryId));
        Assert.Collection(result.Value.Expenses,
            expense => Assert.Equal(3000, expense.AmountCents),
            expense => Assert.Equal(1500, expense.AmountCents),
            expense => Assert.Equal(500, expense.AmountCents));
    }

    [Fact]
    public async Task Should_return_no_budget_when_no_active_category_has_a_budget()
    {
        var categories = new FakeCategoryRepository();
        categories.Items.Add(new Category(Guid.NewGuid(), "Travel", "car", "#4F5F7A"));

        var result = await new YearlyOverviewService(categories, new FakeExpenseRepository()).Get(2026);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.TotalBudgetCents);
        Assert.Empty(result.Value.Categories);
        Assert.Empty(result.Value.Expenses);
    }

    [Fact]
    public async Task Should_return_only_ten_highest_expenses_with_deterministic_tie_breaking()
    {
        var categories = new FakeCategoryRepository();
        var expenses = new FakeExpenseRepository();
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B");
        categories.Items.Add(category);
        for (var day = 1; day <= 11; day++)
            expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 1, day)));

        var result = await new YearlyOverviewService(categories, expenses).Get(2026);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.Expenses.Count);
        Assert.Equal(new DateOnly(2026, 1, 11), result.Value.Expenses[0].OccurredOn);
        Assert.Equal(new DateOnly(2026, 1, 2), result.Value.Expenses[9].OccurredOn);
    }
}
