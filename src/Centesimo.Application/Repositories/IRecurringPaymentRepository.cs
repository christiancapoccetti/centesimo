using Centesimo.Domain;

namespace Centesimo.Application;

public interface IRecurringPaymentRepository
{
    Task<Result> Add(RecurringPayment payment, CancellationToken cancellationToken = default);
    Task<Result<RecurringPayment?>> Get(Guid recurringPaymentId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RecurringPayment>>> GetAll(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<RecurringPayment>>> GetDue(DateOnly through,
        CancellationToken cancellationToken = default);
    Task<Result> Update(RecurringPayment payment, CancellationToken cancellationToken = default);
}


