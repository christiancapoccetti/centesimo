using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class MonthlyOverviewService_should_expected_behavior
{
    [Fact]
    public async Task Should_build_selected_month_overview_with_all_month_expenses()
    {
        var categories = new FakeCategoryRepository();
        var expenses = new FakeExpenseRepository();
        var groceries = new Category(Guid.NewGuid(), "Groceries", "cart", "#176B5B", new Money(20000));
        var archived = new Category(Guid.NewGuid(), "Old", "more", "#6F7975", new Money(10000));
        archived.Archive();
        categories.Items.AddRange([groceries, archived]);
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), groceries.CategoryId, new Money(1250), new DateOnly(2026, 7, 20), note: "Lunch"),
            new Expense(Guid.NewGuid(), groceries.CategoryId, new Money(750), new DateOnly(2026, 7, 2)),
            new Expense(Guid.NewGuid(), archived.CategoryId, new Money(500), new DateOnly(2026, 7, 10)),
            new Expense(Guid.NewGuid(), groceries.CategoryId, new Money(900), new DateOnly(2026, 6, 30))
        ]);
        var service = new MonthlyOverviewService(categories, expenses);

        var result = await service.Get(2026, 7);

        Assert.True(result.IsSuccess);
        Assert.Equal(2500, result.Value.SpentCents);
        Assert.Equal(20000, result.Value.TotalBudgetCents);
        var category = Assert.Single(result.Value.Categories);
        Assert.Equal(2000, category.SpentCents);
        Assert.Equal(3, result.Value.Expenses.Count);
        Assert.Equal(new DateOnly(2026, 7, 20), result.Value.Expenses[0].OccurredOn);
        Assert.DoesNotContain(result.Value.Expenses, expense => expense.AmountCents == 900);
    }

    [Fact]
    public async Task Should_return_empty_selected_month_and_no_budget_when_none_are_configured()
    {
        var categories = new FakeCategoryRepository();
        categories.Items.Add(new Category(Guid.NewGuid(), "Travel", "car", "#4F5F7A"));
        var service = new MonthlyOverviewService(categories, new FakeExpenseRepository());

        var result = await service.Get(2026, 5);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.TotalBudgetCents);
        Assert.Empty(result.Value.Expenses);
        Assert.Equal(0, result.Value.SpentCents);
    }

    [Fact]
    public async Task Should_show_spending_categories_in_descending_order_and_only_five_latest_expenses()
    {
        var categories = new FakeCategoryRepository();
        var expenses = new FakeExpenseRepository();
        var food = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B");
        var travel = new Category(Guid.NewGuid(), "Travel", "car", "#4F5F7A");
        var unused = new Category(Guid.NewGuid(), "Unused", "more", "#6F7975");
        categories.Items.AddRange([food, travel, unused]);
        for (var day = 1; day <= 7; day++)
            expenses.Items.Add(new Expense(Guid.NewGuid(), food.CategoryId, new Money(100), new DateOnly(2026, 7, day)));
        expenses.Items.Add(new Expense(Guid.NewGuid(), travel.CategoryId, new Money(1000), new DateOnly(2026, 7, 1)));
        var service = new MonthlyOverviewService(categories, expenses);

        var result = await service.Get(2026, 7);

        Assert.True(result.IsSuccess);
        Assert.Collection(result.Value.Categories,
            category => Assert.Equal(travel.CategoryId, category.CategoryId),
            category => Assert.Equal(food.CategoryId, category.CategoryId));
        Assert.DoesNotContain(result.Value.Categories, category => category.CategoryId == unused.CategoryId);
        Assert.Equal(5, result.Value.Expenses.Count);
        Assert.Equal(new DateOnly(2026, 7, 7), result.Value.Expenses[0].OccurredOn);
        Assert.Equal(new DateOnly(2026, 7, 3), result.Value.Expenses[4].OccurredOn);
    }}