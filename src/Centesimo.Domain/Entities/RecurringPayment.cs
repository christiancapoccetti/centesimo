namespace Centesimo.Domain;

public sealed class RecurringPayment
{
    public Guid RecurringPaymentId { get; }
    public Guid CategoryId { get; private set; }
    public Guid? TagId { get; private set; }
    public Money Amount { get; private set; }
    public string Note { get; private set; } = "";
    public RecurrenceFrequency Frequency { get; private set; }
    public DateOnly NextDueOn { get; private set; }
    public DateOnly? EndsOn { get; private set; }
    public bool IsSuspended { get; private set; }
    public int AnchorMonth { get; private set; }
    public int AnchorDay { get; private set; }

    public RecurringPayment(Guid recurringPaymentId, Guid categoryId, Money amount,
        RecurrenceFrequency frequency, DateOnly nextDueOn, Guid? tagId = null,
        string note = "", DateOnly? endsOn = null)
    {
        if (recurringPaymentId == Guid.Empty || categoryId == Guid.Empty)
            throw new ArgumentException("Recurring payment and category IDs are required.");

        if (amount.Cents == 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        RecurringPaymentId = recurringPaymentId;
        AnchorMonth = nextDueOn.Month;
        AnchorDay = nextDueOn.Day;
        Update(categoryId, amount, frequency, nextDueOn, tagId, note, endsOn);
    }

    public void Update(Guid categoryId, Money amount, RecurrenceFrequency frequency,
        DateOnly nextDueOn, Guid? tagId, string note, DateOnly? endsOn)
    {
        if (categoryId == Guid.Empty || amount.Cents == 0)
            throw new ArgumentException("Category and positive amount are required.");

        if (endsOn < nextDueOn)
            throw new ArgumentException("End date cannot precede the next due date.", nameof(endsOn));

        CategoryId = categoryId;
        Amount = amount;
        Frequency = frequency;
        NextDueOn = nextDueOn;
        TagId = tagId;
        Note = note.Trim();
        EndsOn = endsOn;
        AnchorMonth = nextDueOn.Month;
        AnchorDay = nextDueOn.Day;
    }

    public void Suspend() => IsSuspended = true;
    public void Resume() => IsSuspended = false;
    public void End(DateOnly endsOn) => EndsOn = endsOn;

    public void MoveToNextOccurrence()
    {
        NextDueOn = new RecurrenceDefinition(Frequency, AnchorMonth, AnchorDay)
            .GetNext(NextDueOn);
    }
}


