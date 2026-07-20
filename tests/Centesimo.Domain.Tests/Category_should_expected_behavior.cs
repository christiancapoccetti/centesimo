using Centesimo.Domain;

namespace Centesimo.Domain.Tests;

public sealed class Category_should_expected_behavior
{
    [Fact]
    public void Set_optional_monthly_budget_and_archive()
    {
        var category = new Category(Guid.NewGuid(), " Groceries ", "cart", "#123456");

        category.SetBudget(new Money(20_000));
        category.Archive();

        Assert.Equal("Groceries", category.Name);
        Assert.Equal(20_000, category.MonthlyBudget?.Cents);
        Assert.True(category.IsArchived);
    }
}
