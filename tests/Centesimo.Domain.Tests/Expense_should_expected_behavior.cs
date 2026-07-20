using Centesimo.Domain;

namespace Centesimo.Domain.Tests;

public sealed class Expense_should_expected_behavior
{
    [Fact]
    public void Store_required_and_optional_details()
    {
        var categoryId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var expense = new Expense(Guid.NewGuid(), categoryId, new Money(999),
            new DateOnly(2026, 7, 20), tagId, " Lunch ", "photo.jpg");

        Assert.Equal(categoryId, expense.CategoryId);
        Assert.Equal(tagId, expense.TagId);
        Assert.Equal("Lunch", expense.Note);
    }

    [Fact]
    public void Reject_zero_amount() => Assert.Throws<ArgumentOutOfRangeException>(() =>
        new Expense(Guid.NewGuid(), Guid.NewGuid(), new Money(0), DateOnly.FromDateTime(DateTime.Today)));
}
