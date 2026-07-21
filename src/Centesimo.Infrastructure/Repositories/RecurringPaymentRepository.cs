using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

public sealed class RecurringPaymentRepository(CentesimoDbContext context)
    : RepositoryBase(context), IRecurringPaymentRepository
{
    public Task<Result> Add(RecurringPayment payment, CancellationToken cancellationToken = default) =>
        SaveContext(db => db.RecurringPayments.Add(payment), cancellationToken);

    public Task<Result<RecurringPayment?>> Get(Guid recurringPaymentId,
        CancellationToken cancellationToken = default) =>
        UseContext((db, token) => db.RecurringPayments.AsNoTracking()
            .SingleOrDefaultAsync(payment => payment.RecurringPaymentId == recurringPaymentId, token),
            cancellationToken);

    public Task<Result<IReadOnlyList<RecurringPayment>>> GetAll(
        CancellationToken cancellationToken = default) =>
        UseContext<IReadOnlyList<RecurringPayment>>(async (db, token) =>
            await db.RecurringPayments.AsNoTracking()
                .OrderBy(payment => payment.NextDueOn)
                .ToListAsync(token), cancellationToken);

    public Task<Result<IReadOnlyList<RecurringPayment>>> GetDue(DateOnly throughDate,
        CancellationToken cancellationToken = default) =>
        UseContext<IReadOnlyList<RecurringPayment>>(async (db, token) =>
            await db.RecurringPayments.AsNoTracking()
                .Where(payment => !payment.IsSuspended
                    && payment.NextDueOn <= throughDate
                    && (!payment.EndsOn.HasValue || payment.NextDueOn <= payment.EndsOn.Value))
                .ToListAsync(token), cancellationToken);

    public Task<Result> Update(RecurringPayment payment,
        CancellationToken cancellationToken = default) =>
        SaveContext(async (db, token) =>
        {
            var tracked = await db.RecurringPayments.SingleOrDefaultAsync(
                item => item.RecurringPaymentId == payment.RecurringPaymentId, token);
            if (tracked is null)
                return;

            db.Entry(tracked).CurrentValues.SetValues(payment);
        }, cancellationToken);
}
