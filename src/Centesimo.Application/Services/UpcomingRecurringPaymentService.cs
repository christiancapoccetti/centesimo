using Centesimo.Domain;

namespace Centesimo.Application;

public sealed record UpcomingRecurringPaymentReminder(Guid RecurringPaymentId, DateOnly DueOn,
    long AmountCents, string NotificationId);

public sealed class UpcomingRecurringPaymentService(IRecurringPaymentRepository recurringPayments)
{
    public async Task<Result<IReadOnlyList<UpcomingRecurringPaymentReminder>>> GetUpcoming(DateOnly today,
        int daysAhead = 7, CancellationToken cancellationToken = default)
    {
        if (daysAhead < 0)
            return Result<IReadOnlyList<UpcomingRecurringPaymentReminder>>.Failure(ApplicationErrors.InvalidEndDate);

        var payments = await recurringPayments.GetAll(cancellationToken);
        if (payments.IsFailure)
            return Result<IReadOnlyList<UpcomingRecurringPaymentReminder>>.Failure(payments.Error);

        var lastDay = today.AddDays(daysAhead);
        var reminders = payments.Value
            .Where(payment => !payment.IsSuspended
                && payment.NextDueOn >= today
                && payment.NextDueOn <= lastDay
                && payment.NextDueOn <= payment.EndsOn.GetValueOrDefault(DateOnly.MaxValue))
            .Select(payment => new UpcomingRecurringPaymentReminder(payment.RecurringPaymentId,
                payment.NextDueOn, payment.Amount.Cents,
                $"recurring-{payment.RecurringPaymentId:N}-{payment.NextDueOn:yyyyMMdd}"))
            .ToList();
        return Result<IReadOnlyList<UpcomingRecurringPaymentReminder>>.Success(reminders);
    }
}
