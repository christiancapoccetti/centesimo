using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

public sealed class ExpenseRepository(CentesimoDbContext context)
    : RepositoryBase(context), IExpenseRepository
{
    public Task<Result<Expense?>> Get(Guid expenseId, CancellationToken cancellationToken = default) =>
        UseContext((db, token) => db.Expenses.AsNoTracking()
            .SingleOrDefaultAsync(expense => expense.ExpenseId == expenseId, token), cancellationToken);

    public Task<Result<IReadOnlyList<Expense>>> GetByCategoryBetween(Guid categoryId, DateOnly from,
        DateOnly to, CancellationToken cancellationToken = default) =>
        UseContext<IReadOnlyList<Expense>>(async (db, token) => await db.Expenses.AsNoTracking()
            .Where(expense => expense.CategoryId == categoryId &&
                expense.OccurredOn >= from && expense.OccurredOn <= to)
            .OrderByDescending(expense => expense.OccurredOn)
            .ToListAsync(token), cancellationToken);
    public Task<Result<IReadOnlyList<Expense>>> GetBetween(DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default) =>
        UseContext<IReadOnlyList<Expense>>(async (db, token) => await db.Expenses.AsNoTracking()
            .Where(expense => expense.OccurredOn >= from && expense.OccurredOn <= to)
            .OrderByDescending(expense => expense.OccurredOn).ToListAsync(token), cancellationToken);

    public Task<Result> Add(Expense expense, CancellationToken cancellationToken = default) =>
        SaveContext(db => db.Expenses.Add(expense), cancellationToken);

    public Task<Result> Update(Expense expense, CancellationToken cancellationToken = default) =>
        SaveContext(async (db, token) =>
        {
            var tracked = await db.Expenses.SingleOrDefaultAsync(
                item => item.ExpenseId == expense.ExpenseId, token);
            if (tracked is null)
                return;

            db.Entry(tracked).CurrentValues.SetValues(expense);
        }, cancellationToken);

    public Task<Result> Delete(Guid expenseId, CancellationToken cancellationToken = default) =>
        SaveContext(async (db, token) =>
        {
            var tracked = await db.Expenses.SingleOrDefaultAsync(
                expense => expense.ExpenseId == expenseId, token);
            if (tracked is null)
                return;

            db.Expenses.Remove(tracked);
        }, cancellationToken);
}
