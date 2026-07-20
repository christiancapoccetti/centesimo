using Centesimo.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centesimo.Infrastructure.Tests;

public sealed class RecurringOccurrenceProcessor_should_expected_behavior
{
    [Fact]
    public async Task Persist_occurrence_expense_and_advanced_payment_together()
    {
        using var database = new TestDatabase();
        var category = new Category(Guid.NewGuid(), "Bills", "receipt", "#123456");
        var payment = new RecurringPayment(Guid.NewGuid(), category.CategoryId, new Money(100),
            RecurrenceFrequency.Monthly, new DateOnly(2026, 1, 31));
        database.Context.AddRange(category, payment);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        payment.MoveToNextOccurrence();
        var occurrence = new RecurrenceOccurrence(payment.RecurringPaymentId, new DateOnly(2026, 1, 31));
        var expense = new Expense(Guid.NewGuid(), category.CategoryId, new Money(100), occurrence.DueOn);

        var created = await new RecurringOccurrenceProcessor(database.Context)
            .Process(payment, expense, occurrence);
        database.Context.ChangeTracker.Clear();

        Assert.True(created.Value);
        Assert.Single(await database.Context.RecurrenceOccurrences.ToListAsync());
        Assert.Single(await database.Context.Expenses.ToListAsync());
        Assert.Equal(new DateOnly(2026, 2, 28),
            (await database.Context.RecurringPayments.SingleAsync()).NextDueOn);
    }

    [Fact]
    public async Task Roll_back_all_changes_when_expense_cannot_be_saved()
    {
        using var database = new TestDatabase();
        var category = new Category(Guid.NewGuid(), "Bills", "receipt", "#123456");
        var payment = new RecurringPayment(Guid.NewGuid(), category.CategoryId, new Money(100),
            RecurrenceFrequency.Monthly, new DateOnly(2026, 1, 31));
        database.Context.AddRange(category, payment);
        await database.Context.SaveChangesAsync();
        database.Context.ChangeTracker.Clear();
        payment.MoveToNextOccurrence();
        var occurrence = new RecurrenceOccurrence(payment.RecurringPaymentId, new DateOnly(2026, 1, 31));
        var invalidExpense = new Expense(Guid.NewGuid(), Guid.NewGuid(), new Money(100), occurrence.DueOn);
        var processor = new RecurringOccurrenceProcessor(database.Context);

        var failed = await processor.Process(payment, invalidExpense, occurrence);
        Assert.Equal("Infrastructure.PersistenceFailure", failed.Error.Code);
        database.Context.ChangeTracker.Clear();

        Assert.Empty(await database.Context.RecurrenceOccurrences.ToListAsync());
        Assert.Empty(await database.Context.Expenses.ToListAsync());
        Assert.Equal(new DateOnly(2026, 1, 31),
            (await database.Context.RecurringPayments.SingleAsync()).NextDueOn);
    }
}


