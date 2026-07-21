using Centesimo.Domain;

namespace Centesimo.Application;

public sealed record SaveRecurringPaymentRequest(Guid CategoryId, long AmountCents,
    RecurrenceFrequency Frequency, DateOnly NextDueOn, Guid? TagId = null,
    string Note = "", DateOnly? EndsOn = null);

public sealed class RecurringPaymentService(
    ICategoryRepository categories,
    ITagRepository tags,
    IExpenseRepository expenses,
    IRecurringPaymentRepository recurringPayments,
    IRecurringOccurrenceProcessor occurrenceProcessor)
{
    public Task<Result<IReadOnlyList<RecurringPayment>>> GetAll(
        CancellationToken cancellationToken = default) => recurringPayments.GetAll(cancellationToken);

    public Task<Result<RecurringPayment?>> Get(Guid paymentId,
        CancellationToken cancellationToken = default) => recurringPayments.Get(paymentId, cancellationToken);

    public async Task<Result<RecurringPayment>> Create(SaveRecurringPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await Validate(request, cancellationToken);
        if (validation.IsFailure)
            return Result<RecurringPayment>.Failure(validation.Error);

        var payment = request.ToRecurringPayment(Guid.NewGuid());
        var saved = await recurringPayments.Add(payment, cancellationToken);
        return saved.IsFailure
            ? Result<RecurringPayment>.Failure(saved.Error)
            : Result<RecurringPayment>.Success(payment);
    }

    public async Task<Result> Update(Guid paymentId, SaveRecurringPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var found = await recurringPayments.Get(paymentId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.RecurringPaymentNotFound);

        var validation = await Validate(request, cancellationToken);
        if (validation.IsFailure)
            return validation;

        found.Value.Update(request.CategoryId, new Money(request.AmountCents), request.Frequency,
            request.NextDueOn, request.TagId, request.Note, request.EndsOn);
        return await recurringPayments.Update(found.Value, cancellationToken);
    }

    public Task<Result> Suspend(Guid paymentId, CancellationToken cancellationToken = default) =>
        Change(paymentId, payment => payment.Suspend(), cancellationToken);

    public Task<Result> Resume(Guid paymentId, CancellationToken cancellationToken = default) =>
        Change(paymentId, payment => payment.Resume(), cancellationToken);

    public async Task<Result> End(Guid paymentId, DateOnly endsOn,
        CancellationToken cancellationToken = default)
    {
        var found = await recurringPayments.Get(paymentId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.RecurringPaymentNotFound);

        if (endsOn < found.Value.NextDueOn)
            return Result.Failure(ApplicationErrors.InvalidEndDate);

        found.Value.End(endsOn);
        return await recurringPayments.Update(found.Value, cancellationToken);
    }

    public async Task<Result<int>> ProcessDue(DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var dueResult = await recurringPayments.GetDue(today, cancellationToken);
        if (dueResult.IsFailure)
            return Result<int>.Failure(dueResult.Error);

        var created = 0;
        foreach (var payment in dueResult.Value)
        {
            while (!payment.IsSuspended
                && payment.NextDueOn <= today
                && payment.NextDueOn <= payment.EndsOn.GetValueOrDefault(DateOnly.MaxValue))
            {
                var dueOn = payment.NextDueOn;
                var occurrence = new RecurrenceOccurrence(payment.RecurringPaymentId, dueOn);
                var request = new SaveExpenseRequest(payment.CategoryId, payment.Amount.Cents,
                    dueOn, payment.TagId, payment.Note);
                var expense = request.ToExpense(Guid.NewGuid(), payment.RecurringPaymentId);
                payment.MoveToNextOccurrence();
                var processed = await occurrenceProcessor.Process(
                    payment, expense, occurrence, cancellationToken);
                if (processed.IsFailure)
                    return Result<int>.Failure(processed.Error);

                if (processed.Value)
                    created++;
            }
        }

        return Result<int>.Success(created);
    }

    private async Task<Result> Change(Guid paymentId, Action<RecurringPayment> change,
        CancellationToken cancellationToken)
    {
        var found = await recurringPayments.Get(paymentId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.RecurringPaymentNotFound);

        change(found.Value);
        return await recurringPayments.Update(found.Value, cancellationToken);
    }

    private async Task<Result> Validate(SaveRecurringPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EndsOn < request.NextDueOn)
            return Result.Failure(ApplicationErrors.InvalidEndDate);

        var expenseValidation = new ExpenseService(categories, tags, expenses);
        var validationRequest = new SaveExpenseRequest(request.CategoryId, request.AmountCents,
            request.NextDueOn, request.TagId, request.Note);
        return await expenseValidation.ValidateForRecurringPayment(validationRequest, cancellationToken);
    }
}
