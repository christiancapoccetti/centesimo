namespace Centesimo.Application;

public static class MonthlyNavigation
{
    public static DateOnly Previous(DateOnly selectedMonth) =>
        FirstDay(selectedMonth).AddMonths(-1);

    public static bool CanGoNext(DateOnly selectedMonth, DateOnly currentDate) =>
        FirstDay(selectedMonth) < FirstDay(currentDate);

    public static DateOnly Next(DateOnly selectedMonth, DateOnly currentDate) =>
        CanGoNext(selectedMonth, currentDate)
            ? FirstDay(selectedMonth).AddMonths(1)
            : FirstDay(selectedMonth);

    private static DateOnly FirstDay(DateOnly value) => new(value.Year, value.Month, 1);
}
