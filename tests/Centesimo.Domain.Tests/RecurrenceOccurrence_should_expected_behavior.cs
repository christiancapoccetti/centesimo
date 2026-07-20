using Centesimo.Domain;

namespace Centesimo.Domain.Tests;

public sealed class RecurrenceOccurrence_should_expected_behavior
{
    [Fact]
    public void Identify_occurrence_by_payment_and_due_date()
    {
        var paymentId = Guid.NewGuid();
        var occurrence = new RecurrenceOccurrence(paymentId, new DateOnly(2026, 7, 20));

        Assert.Equal(paymentId, occurrence.RecurringPaymentId);
        Assert.Equal(new DateOnly(2026, 7, 20), occurrence.DueOn);
    }
}
