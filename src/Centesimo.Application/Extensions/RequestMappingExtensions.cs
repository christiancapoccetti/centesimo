using Centesimo.Domain;

namespace Centesimo.Application;

public static class RequestMappingExtensions
{
    public static Expense ToExpense(this SaveExpenseRequest request, Guid expenseId,
        Guid? recurringPaymentId = null) =>
        new(expenseId, request.CategoryId, new Money(request.AmountCents), request.OccurredOn,
            request.TagId, request.Note, request.PhotoPath, recurringPaymentId);

    public static RecurringPayment ToRecurringPayment(this SaveRecurringPaymentRequest request,
        Guid recurringPaymentId) =>
        new(recurringPaymentId, request.CategoryId, new Money(request.AmountCents), request.Frequency,
            request.NextDueOn, request.TagId, request.Note, request.EndsOn);
}
