namespace Centesimo.Domain;

public sealed class RecurrenceDefinition
{
    public RecurrenceFrequency Frequency { get; }

    public RecurrenceDefinition(RecurrenceFrequency frequency) => Frequency = frequency;

    public DateOnly GetNext(DateOnly current) => Frequency switch
    {
        RecurrenceFrequency.Weekly => current.AddDays(7),
        RecurrenceFrequency.Monthly => current.AddMonths(1),
        RecurrenceFrequency.Yearly => current.AddYears(1),
        _ => throw new InvalidOperationException("Unsupported recurrence frequency.")
    };
}
