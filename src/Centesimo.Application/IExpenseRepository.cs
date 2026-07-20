using Centesimo.Domain;

namespace Centesimo.Application;

public interface IExpenseRepository
{
    Task<Result> Add(Expense expense, CancellationToken cancellationToken = default);
    Task<Result<Expense?>> Get(Guid expenseId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Expense>>> GetBetween(DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default);
    Task<Result> Update(Expense expense, CancellationToken cancellationToken = default);
    Task<Result> Delete(Guid expenseId, CancellationToken cancellationToken = default);
}
