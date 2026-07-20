using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RequestMappingExtensions_should_expected_behavior
{
    [Fact]
    public void Map_expense_request_with_optional_values()
    {
        var categoryId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var expenseId = Guid.NewGuid();
        var request = new SaveExpenseRequest(categoryId, 1234, new DateOnly(2026, 7, 20),
            tagId, "Note", "photo.jpg");

        var expense = request.ToExpense(expenseId);

        Assert.Equal(expenseId, expense.ExpenseId);
        Assert.Equal(1234, expense.Amount.Cents);
        Assert.Equal(tagId, expense.TagId);
        Assert.Equal("photo.jpg", expense.PhotoPath);
    }

    [Fact]
    public void Map_recurring_payment_request_and_anchor()
    {
        var request = new SaveRecurringPaymentRequest(Guid.NewGuid(), 2000,
            RecurrenceFrequency.Monthly, new DateOnly(2026, 1, 31));

        var payment = request.ToRecurringPayment(Guid.NewGuid());

        Assert.Equal(31, payment.AnchorDay);
        Assert.Equal(1, payment.AnchorMonth);
        Assert.Equal(2000, payment.Amount.Cents);
    }
}

