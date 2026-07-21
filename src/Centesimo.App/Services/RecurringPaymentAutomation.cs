using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

public interface IRecurringPaymentReminder
{
    Task Resync(IReadOnlyList<UpcomingRecurringPaymentReminder> reminders);
}

public sealed class NoOpRecurringPaymentReminder : IRecurringPaymentReminder
{
    public Task Resync(IReadOnlyList<UpcomingRecurringPaymentReminder> reminders) => Task.CompletedTask;
}

public sealed class RecurringPaymentAutomation(IServiceScopeFactory scopeFactory,
    IRecurringPaymentReminder reminder)
{
    private readonly SemaphoreSlim _processing = new(1, 1);

    public async Task ProcessDue()
    {
        if (!await _processing.WaitAsync(0))
            return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var today = DateOnly.FromDateTime(DateTime.Today);
            var service = scope.ServiceProvider.GetRequiredService<RecurringPaymentService>();
            var processed = await service.ProcessDue(today);
            if (processed.IsFailure)
                return;

            var upcoming = scope.ServiceProvider.GetRequiredService<UpcomingRecurringPaymentService>();
            var reminders = await upcoming.GetUpcoming(today);
            if (reminders.IsFailure)
                return;

            await reminder.Resync(reminders.Value);
        }
        catch
        {
        }
        finally
        {
            _processing.Release();
        }
    }
}
