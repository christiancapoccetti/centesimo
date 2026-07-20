using Centesimo.Domain;

namespace Centesimo.Application;

public interface IExpenseRepository
{
    Task Add(Expense expense, CancellationToken cancellationToken = default);
    Task<Expense?> Get(Guid expenseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Expense>> GetBetween(DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default);
    Task Update(Expense expense, CancellationToken cancellationToken = default);
    Task Delete(Guid expenseId, CancellationToken cancellationToken = default);
}
