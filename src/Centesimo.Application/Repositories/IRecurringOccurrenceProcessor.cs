using Centesimo.Domain;

namespace Centesimo.Application;

public interface IRecurringOccurrenceProcessor
{
    Task<Result<bool>> Process(
        RecurringPayment advancedPayment,
        Expense expense,
        RecurrenceOccurrence occurrence,
        CancellationToken cancellationToken = default);
}

