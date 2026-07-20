using Centesimo.Domain;

namespace Centesimo.Domain.Tests;

public sealed class RecurrenceDefinition_should_expected_behavior
{
    [Theory]
    [InlineData(RecurrenceFrequency.Weekly, 2026, 1, 8)]
    [InlineData(RecurrenceFrequency.Monthly, 2026, 2, 1)]
    [InlineData(RecurrenceFrequency.Yearly, 2027, 1, 1)]
    public void Calculate_next_occurrence(RecurrenceFrequency frequency, int year, int month, int day)
    {
        var recurrence = new RecurrenceDefinition(frequency);

        var next = recurrence.GetNext(new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(year, month, day), next);
    }

    [Fact]
    public void Keep_end_of_month_semantics()
    {
        var recurrence = new RecurrenceDefinition(RecurrenceFrequency.Monthly);

        Assert.Equal(new DateOnly(2026, 2, 28), recurrence.GetNext(new DateOnly(2026, 1, 31)));
    }
}
