namespace Centesimo.Application.Tests;

public sealed class MonthlyNavigation_should_expected_behavior
{
    [Fact]
    public void Should_navigate_months_without_moving_into_the_future()
    {
        var currentDate = new DateOnly(2026, 7, 20);
        var currentMonth = new DateOnly(2026, 7, 1);
        var previous = MonthlyNavigation.Previous(currentMonth);

        Assert.Equal(new DateOnly(2026, 6, 1), previous);
        Assert.True(MonthlyNavigation.CanGoNext(previous, currentDate));
        Assert.Equal(currentMonth, MonthlyNavigation.Next(previous, currentDate));
        Assert.False(MonthlyNavigation.CanGoNext(currentMonth, currentDate));
        Assert.Equal(currentMonth, MonthlyNavigation.Next(currentMonth, currentDate));
    }
}
