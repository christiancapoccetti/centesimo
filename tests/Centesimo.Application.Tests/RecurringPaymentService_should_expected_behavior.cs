using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed partial class RecurringPaymentService_should_expected_behavior
{
    [Fact]
    public async Task Catch_up_missed_occurrences_and_remain_idempotent()
    {
        var categories = new FakeCategoryRepository();
        var category = new Category(Guid.NewGuid(), "Bills", "receipt", "#123456");
        categories.Items.Add(category);
        var expenses = new FakeExpenseRepository();
        var payments = new FakeRecurringPaymentRepository();
        var service = new RecurringPaymentService(categories, new FakeTagRepository(), expenses, payments,
            new FakeRecurringOccurrenceProcessor(expenses));
        await service.Create(new(category.CategoryId, 1_000, RecurrenceFrequency.Monthly, new DateOnly(2026, 5, 1)));

        var firstRun = await service.ProcessDue(new DateOnly(2026, 7, 20));
        var secondRun = await service.ProcessDue(new DateOnly(2026, 7, 20));

        Assert.Equal(3, firstRun.Value);
        Assert.Equal(0, secondRun.Value);
        Assert.Equal(3, expenses.Items.Count);
    }

    [Fact]
    public async Task Suspend_update_and_end_payment()
    {
        var categories = new FakeCategoryRepository();
        var category = new Category(Guid.NewGuid(), "Bills", "receipt", "#123456");
        categories.Items.Add(category);
        var payments = new FakeRecurringPaymentRepository();
        var expenses = new FakeExpenseRepository();
        var service = new RecurringPaymentService(categories, new FakeTagRepository(), expenses, payments,
            new FakeRecurringOccurrenceProcessor(expenses));
        var created = await service.Create(new(category.CategoryId, 1_000, RecurrenceFrequency.Weekly, new DateOnly(2026, 7, 20)));

        var updated = await service.Update(created.Value.RecurringPaymentId, new(category.CategoryId, 2_000, RecurrenceFrequency.Yearly, new DateOnly(2026, 8, 1)));
        var suspended = await service.Suspend(created.Value.RecurringPaymentId);
        var ended = await service.End(created.Value.RecurringPaymentId, new DateOnly(2027, 1, 1));

        Assert.True(updated.IsSuccess && suspended.IsSuccess && ended.IsSuccess);
        Assert.True(created.Value.IsSuspended);
        Assert.Equal(RecurrenceFrequency.Yearly, created.Value.Frequency);
    }

    [Fact]
    public async Task Resume_suspended_payment_and_reject_invalid_end_date()
    {
        var categories = new FakeCategoryRepository();
        var category = new Category(Guid.NewGuid(), "Bills", "receipt", "#123456");
        categories.Items.Add(category);
        var expenses = new FakeExpenseRepository();
        var payments = new FakeRecurringPaymentRepository();
        var service = new RecurringPaymentService(categories, new FakeTagRepository(), expenses,
            payments, new FakeRecurringOccurrenceProcessor(expenses));
        var created = await service.Create(new(category.CategoryId, 1_000, RecurrenceFrequency.Monthly, new DateOnly(2026, 7, 20)));
        await service.Suspend(created.Value.RecurringPaymentId);

        var resumed = await service.Resume(created.Value.RecurringPaymentId);
        var invalidEnd = await service.End(created.Value.RecurringPaymentId, new DateOnly(2026, 7, 19));

        Assert.True(resumed.IsSuccess);
        Assert.False(created.Value.IsSuspended);
        Assert.Equal("RecurringPayment.InvalidEndDate", invalidEnd.Error.Code);
    }

    [Fact]
    public async Task Return_recurring_payments_ordered_by_next_due_date()
    {
        var repository = new FakeRecurringPaymentRepository();
        var categoryId = Guid.NewGuid();
        repository.Items.Add(new(Guid.NewGuid(), categoryId, new Money(100), RecurrenceFrequency.Monthly,
            new DateOnly(2026, 9, 1)));
        repository.Items.Add(new(Guid.NewGuid(), categoryId, new Money(200), RecurrenceFrequency.Weekly,
            new DateOnly(2026, 8, 1)));
        var service = new RecurringPaymentService(new FakeCategoryRepository(), new FakeTagRepository(),
            new FakeExpenseRepository(), repository, new FakeRecurringOccurrenceProcessor(new FakeExpenseRepository()));

        var result = await service.GetAll();

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 8, 1), result.Value[0].NextDueOn);
    }
}
