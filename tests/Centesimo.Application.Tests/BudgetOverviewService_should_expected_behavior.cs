using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class BudgetOverviewService_should_expected_behavior
{
    [Fact]
    public async Task Calculate_monthly_totals_and_percentage()
    {
        var categories = new FakeCategoryRepository();
        var expenses = new FakeExpenseRepository();
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#123456", new Money(10_000));
        categories.Items.Add(category);
        expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(2_500), new DateOnly(2026, 7, 10)));

        var overview = await new BudgetOverviewService(categories, expenses).GetMonth(2026, 7);

        Assert.Equal(2_500, overview.Value[0].SpentCents);
        Assert.Equal(25m, overview.Value[0].PercentageUsed);
        Assert.Equal(BudgetStatus.OnTrack, overview.Value[0].Status);
    }
}



