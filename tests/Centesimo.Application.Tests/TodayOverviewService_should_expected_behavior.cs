using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class TodayOverviewService_should_expected_behavior
{
    [Fact]
    public async Task Should_build_active_category_budget_and_today_expense_overview()
    {
        var categories = new FakeCategoryRepository();
        var expenses = new FakeExpenseRepository();
        var groceries = new Category(Guid.NewGuid(), "Groceries", "cart", "#176B5B", new Money(20000));
        var archived = new Category(Guid.NewGuid(), "Old", "more", "#6F7975", new Money(10000));
        archived.Archive();
        categories.Items.AddRange([groceries, archived]);
        var today = new DateOnly(2026, 7, 20);
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), groceries.CategoryId, new Money(1250), today, note: "Lunch"),
            new Expense(Guid.NewGuid(), groceries.CategoryId, new Money(750), today.AddDays(-1)),
            new Expense(Guid.NewGuid(), archived.CategoryId, new Money(500), today)
        ]);
        var service = new TodayOverviewService(categories, expenses);

        var result = await service.Get(today);

        Assert.True(result.IsSuccess);
        Assert.Equal(2500, result.Value.MonthlySpentCents);
        Assert.Equal(20000, result.Value.TotalBudgetCents);
        var category = Assert.Single(result.Value.Categories);
        Assert.Equal(groceries.CategoryId, category.CategoryId);
        Assert.Equal(2000, category.SpentCents);
        Assert.Equal(2, result.Value.Expenses.Count);
        Assert.Contains(result.Value.Expenses, expense => expense.Note == "Lunch");
    }

    [Fact]
    public async Task Should_return_no_total_budget_when_none_are_configured()
    {
        var categories = new FakeCategoryRepository();
        categories.Items.Add(new Category(Guid.NewGuid(), "Travel", "car", "#4F5F7A"));
        var service = new TodayOverviewService(categories, new FakeExpenseRepository());

        var result = await service.Get(new DateOnly(2026, 7, 20));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.TotalBudgetCents);
    }
}
