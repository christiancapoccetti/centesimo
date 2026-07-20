namespace Centesimo.Application;

public static class ApplicationErrors
{
    public static readonly Error CategoryNotFound = new("Category.NotFound", "La categoria non esiste.");
    public static readonly Error CategoryNameAlreadyExists = new("Category.NameAlreadyExists", "Esiste già una categoria con questo nome.");
    public static readonly Error CategoryArchived = new("Category.Archived", "La categoria è archiviata.");
    public static readonly Error TagNotFound = new("Tag.NotFound", "Il tag non esiste.");
    public static readonly Error TagArchived = new("Tag.Archived", "Il tag è archiviato.");
    public static readonly Error TagCategoryMismatch = new("Tag.CategoryMismatch", "Il tag non appartiene alla categoria.");
    public static readonly Error ExpenseNotFound = new("Expense.NotFound", "La spesa non esiste.");
    public static readonly Error InvalidAmount = new("Expense.InvalidAmount", "L'importo deve essere maggiore di zero.");
    public static readonly Error InvalidName = new("Validation.InvalidName", "Il nome è obbligatorio.");
    public static readonly Error RecurringPaymentNotFound = new("RecurringPayment.NotFound", "Il pagamento regolare non esiste.");
    public static readonly Error InvalidEndDate = new("RecurringPayment.InvalidEndDate", "La data di fine non è valida.");
}
