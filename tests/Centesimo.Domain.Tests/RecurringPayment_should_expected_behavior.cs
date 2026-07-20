using Centesimo.Domain;

namespace Centesimo.Domain.Tests;

public sealed class RecurringPayment_should_expected_behavior
{
    [Fact]
    public void Preserve_month_end_anchor_across_short_months()
    {
        var payment = Create(RecurrenceFrequency.Monthly, new DateOnly(2026, 1, 31));

        payment.MoveToNextOccurrence();
        Assert.Equal(new DateOnly(2026, 2, 28), payment.NextDueOn);
        payment.MoveToNextOccurrence();

        Assert.Equal(new DateOnly(2026, 3, 31), payment.NextDueOn);
    }

    [Fact]
    public void Preserve_leap_day_anchor_across_non_leap_years()
    {
        var payment = Create(RecurrenceFrequency.Yearly, new DateOnly(2024, 2, 29));

        payment.MoveToNextOccurrence();
        payment.MoveToNextOccurrence();
        payment.MoveToNextOccurrence();
        payment.MoveToNextOccurrence();

        Assert.Equal(new DateOnly(2028, 2, 29), payment.NextDueOn);
    }

    private static RecurringPayment Create(RecurrenceFrequency frequency, DateOnly nextDueOn) =>
        new(Guid.NewGuid(), Guid.NewGuid(), new Money(100), frequency, nextDueOn);
}
