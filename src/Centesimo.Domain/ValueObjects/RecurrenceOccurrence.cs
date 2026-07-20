namespace Centesimo.Domain;

public sealed record RecurrenceOccurrence
{
    public Guid RecurringPaymentId { get; }
    public DateOnly DueOn { get; }

    public RecurrenceOccurrence(Guid recurringPaymentId, DateOnly dueOn)
    {
        if (recurringPaymentId == Guid.Empty)
            throw new ArgumentException("Recurring payment ID is required.", nameof(recurringPaymentId));

        RecurringPaymentId = recurringPaymentId;
        DueOn = dueOn;
    }
}
