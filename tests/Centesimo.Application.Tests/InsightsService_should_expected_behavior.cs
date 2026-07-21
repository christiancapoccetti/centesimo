using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class InsightsService_should_expected_behavior
{
    [Fact]
    public async Task Should_return_empty_overview_when_period_has_no_expenses()
    {
        var service = CreateService(new DateOnly(2026, 7, 20));

        var result = await service.Get(InsightPeriod.Month);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.SpentCents);
        Assert.Null(result.Value.ComparedSpentCents);
        Assert.Empty(result.Value.Categories);
        Assert.Empty(result.Value.Insights);
    }

    [Fact]
    public async Task Should_not_render_category_that_only_has_previous_period_spending()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 6, 20)));

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(InsightPeriod.Month);

        Assert.Empty(result.Value.Categories);
    }

    [Fact]
    public async Task Should_use_italian_trend_labels()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 7, 20)));

        var month = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(InsightPeriod.Month);
        var year = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(InsightPeriod.Year);

        Assert.Contains(month.Value.Trend, x => x.Label == "20 lug");
        Assert.Contains(year.Value.Trend, x => x.Label == "luglio");
    }

    [Fact]
    public async Task Should_compare_current_month_with_same_elapsed_days_of_previous_month()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(800), new DateOnly(2026, 6, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(900), new DateOnly(2026, 6, 25))
        ]);
        var service = CreateService(new DateOnly(2026, 7, 20), category, expenses);

        var result = await service.Get(InsightPeriod.Month);

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 6, 1), result.Value.From.AddMonths(-1));
        Assert.Equal(800, result.Value.ComparedSpentCents);
        Assert.Equal(0.25, result.Value.ChangePercentage!.Value, 3);
    }

    [Fact]
    public async Task Should_not_compare_when_previous_equivalent_period_has_no_expenses()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 7, 20)));

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(InsightPeriod.Month);

        Assert.Null(result.Value.ComparedSpentCents);
        Assert.DoesNotContain(result.Value.Insights, x => x.Kind is InsightKind.Increased or InsightKind.Decreased or InsightKind.Stable);
    }

    [Fact]
    public async Task Should_create_increase_decrease_top_and_stable_insights()
    {
        var food = new Category(Guid.NewGuid(), "Cibo", "food", "#196D61");
        var car = new Category(Guid.NewGuid(), "Auto", "car", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(2200), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), car.CategoryId, new Money(800), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), food.CategoryId, new Money(1800), new DateOnly(2026, 6, 20)),
            new Expense(Guid.NewGuid(), car.CategoryId, new Money(1200), new DateOnly(2026, 6, 20))
        ]);

        var result = await CreateService(new DateOnly(2026, 7, 20), [food, car], expenses).Get(InsightPeriod.Month);

        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.Increased);
        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.Decreased);
        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.TopCategory && x.CategoryId == food.CategoryId);
    }

    [Fact]
    public async Task Should_compare_current_year_with_same_elapsed_days_of_previous_year()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1200), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2025, 7, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(900), new DateOnly(2025, 8, 1))
        ]);

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(InsightPeriod.Year);

        Assert.Equal(1000, result.Value.ComparedSpentCents);
        Assert.Equal(7, result.Value.Trend.Count);
    }

    [Fact]
    public async Task Should_create_stable_insight_when_total_change_is_small()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1050), new DateOnly(2026, 7, 20)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 6, 20))
        ]);

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(InsightPeriod.Month);

        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.Stable);
    }

    [Fact]
    public async Task Should_create_new_habit_and_unusual_expense_insights_from_local_history()
    {
        var category = new Category(Guid.NewGuid(), "Abbonamenti", "tech", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(100), new DateOnly(2026, 7, 2)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(100), new DateOnly(2026, 7, 3)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2026, 7, 4))
        ]);

        var result = await CreateService(new DateOnly(2026, 7, 20), category, expenses).Get(InsightPeriod.Month);

        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.NewHabit);
        Assert.Contains(result.Value.Insights, x => x.Kind == InsightKind.UnusualExpense && x.ExpenseId.HasValue);
    }

    [Fact]
    public async Task Should_compare_march_first_with_matching_calendar_day_after_leap_year()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.AddRange([
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2025, 3, 1)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(700), new DateOnly(2024, 3, 1)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(900), new DateOnly(2024, 2, 29)),
            new Expense(Guid.NewGuid(), category.CategoryId, new Money(300), new DateOnly(2024, 3, 2))
        ]);

        var result = await CreateService(new DateOnly(2025, 3, 1), category, expenses).Get(InsightPeriod.Year);

        Assert.Equal(1600, result.Value.ComparedSpentCents);
    }

    [Fact]
    public async Task Should_not_compare_february_twenty_ninth_when_previous_year_has_no_matching_date()
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2024, 2, 29)));

        var result = await CreateService(new DateOnly(2024, 2, 29), category, expenses).Get(InsightPeriod.Year);

        Assert.Null(result.Value.ComparedSpentCents);
    }

    [Theory]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(31)]
    public async Task Should_not_compare_march_month_end_when_previous_non_leap_february_has_no_matching_day(int day)
    {
        var category = new Category(Guid.NewGuid(), "Spesa", "cart", "#196D61");
        var expenses = new FakeExpenseRepository();
        expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(1000), new DateOnly(2025, 3, day)));

        var result = await CreateService(new DateOnly(2025, 3, day), category, expenses).Get(InsightPeriod.Month);

        Assert.Null(result.Value.ComparedSpentCents);
    }

    private static InsightsService CreateService(DateOnly today, Category? category = null, FakeExpenseRepository? expenses = null) =>
        CreateService(today, category is null ? [] : [category], expenses);

    private static InsightsService CreateService(DateOnly today, IReadOnlyList<Category> categories, FakeExpenseRepository? expenses = null)
    {
        var categoryRepository = new FakeCategoryRepository();
        categoryRepository.Items.AddRange(categories);
        return new InsightsService(categoryRepository, expenses ?? new FakeExpenseRepository(), () => today);
    }
}
