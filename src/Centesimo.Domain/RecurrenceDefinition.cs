namespace Centesimo.Domain;

public sealed class RecurrenceDefinition
{
    public RecurrenceFrequency Frequency { get; }
    public int? AnchorMonth { get; }
    public int? AnchorDay { get; }

    public RecurrenceDefinition(RecurrenceFrequency frequency, DateOnly? anchor = null)
    {
        Frequency = frequency;
        AnchorMonth = anchor?.Month;
        AnchorDay = anchor?.Day;
    }
    public RecurrenceDefinition(RecurrenceFrequency frequency, int anchorMonth, int anchorDay)
    {
        Frequency = frequency;
        AnchorMonth = anchorMonth;
        AnchorDay = anchorDay;
    }


    public DateOnly GetNext(DateOnly current)
    {
        if (Frequency == RecurrenceFrequency.Weekly)
            return current.AddDays(7);

        if (Frequency == RecurrenceFrequency.Monthly)
        {
            var month = new DateOnly(current.Year, current.Month, 1).AddMonths(1);
            var day = Math.Min(AnchorDay ?? current.Day, DateTime.DaysInMonth(month.Year, month.Month));
            return new DateOnly(month.Year, month.Month, day);
        }

        if (Frequency == RecurrenceFrequency.Yearly)
        {
            var year = current.Year + 1;
            var month = AnchorMonth ?? current.Month;
            var day = Math.Min(AnchorDay ?? current.Day, DateTime.DaysInMonth(year, month));
            return new DateOnly(year, month, day);
        }

        throw new InvalidOperationException("Unsupported recurrence frequency.");
    }
}
