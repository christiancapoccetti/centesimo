using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure;

public sealed class ExpenseRepository(CentesimoDbContext dbContext) : IExpenseRepository
{
    public Task<Expense?> Get(Guid expenseId, CancellationToken cancellationToken = default) =>
        dbContext.Expenses
            .AsNoTracking()
            .SingleOrDefaultAsync(expense => expense.ExpenseId == expenseId, cancellationToken);

    public async Task<IReadOnlyList<Expense>> GetBetween(DateOnly from, DateOnly to,
        CancellationToken cancellationToken = default) =>
        await dbContext.Expenses
            .AsNoTracking()
            .Where(expense => expense.OccurredOn >= from && expense.OccurredOn <= to)
            .OrderByDescending(expense => expense.OccurredOn)
            .ToListAsync(cancellationToken);

    public async Task Add(Expense expense, CancellationToken cancellationToken = default)
    {
        dbContext.Expenses.Add(expense);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Update(Expense expense, CancellationToken cancellationToken = default)
    {
        var tracked = await dbContext.Expenses
            .SingleOrDefaultAsync(item => item.ExpenseId == expense.ExpenseId, cancellationToken);

        if (tracked is null)
        {
            return;
        }

        dbContext.Entry(tracked).CurrentValues.SetValues(expense);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Delete(Guid expenseId, CancellationToken cancellationToken = default)
    {
        var tracked = await dbContext.Expenses
            .SingleOrDefaultAsync(expense => expense.ExpenseId == expenseId, cancellationToken);

        if (tracked is null)
        {
            return;
        }

        dbContext.Expenses.Remove(tracked);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
