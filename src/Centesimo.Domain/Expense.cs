namespace Centesimo.Domain;

public sealed class Expense
{
    public Guid ExpenseId { get; }
    public Guid CategoryId { get; }
    public Guid? TagId { get; }
    public Money Amount { get; }
    public DateOnly OccurredOn { get; }
    public string Note { get; }
    public string? PhotoPath { get; }
    public Guid? RecurringPaymentId { get; }

    public Expense(Guid expenseId, Guid categoryId, Money amount, DateOnly occurredOn,
        Guid? tagId = null, string note = "", string? photoPath = null, Guid? recurringPaymentId = null)
    {
        if (expenseId == Guid.Empty || categoryId == Guid.Empty)
        {
            throw new ArgumentException("Expense and category IDs are required.");
        }

        if (amount.Cents == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Expense amount must be greater than zero.");
        }

        ExpenseId = expenseId;
        CategoryId = categoryId;
        Amount = amount;
        OccurredOn = occurredOn;
        TagId = tagId;
        Note = note.Trim();
        PhotoPath = photoPath;
        RecurringPaymentId = recurringPaymentId;
    }
}
