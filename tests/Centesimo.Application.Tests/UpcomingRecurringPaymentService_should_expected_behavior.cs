using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class UpcomingRecurringPaymentService_should_expected_behavior
{
    [Fact]
    public async Task Return_only_active_payments_due_in_the_reminder_window_with_stable_identifier()
    {
        var repository = new FakeRecurringPaymentRepository();
        var payment = new RecurringPayment(Guid.NewGuid(), Guid.NewGuid(), new Money(1_200),
            RecurrenceFrequency.Monthly, new DateOnly(2026, 7, 23));
        repository.Items.Add(payment);
        repository.Items.Add(new(Guid.NewGuid(), Guid.NewGuid(), new Money(100),
            RecurrenceFrequency.Monthly, new DateOnly(2026, 8, 1)));
        var suspended = new RecurringPayment(Guid.NewGuid(), Guid.NewGuid(), new Money(100),
            RecurrenceFrequency.Monthly, new DateOnly(2026, 7, 22));
        suspended.Suspend();
        repository.Items.Add(suspended);

        var result = await new UpcomingRecurringPaymentService(repository).GetUpcoming(new DateOnly(2026, 7, 20));

        var reminder = Assert.Single(result.Value);
        Assert.Equal(payment.RecurringPaymentId, reminder.RecurringPaymentId);
        Assert.Equal($"recurring-{payment.RecurringPaymentId:N}-20260723", reminder.NotificationId);
    }
}
