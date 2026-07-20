namespace Centesimo.Application;

public static class ExpenseErrors
{
    public static readonly Error InvalidAmount = new(
        "Expense.InvalidAmount",
        "L'importo deve essere maggiore di zero.");

    public static readonly Error CategoryNotFound = new(
        "Expense.CategoryNotFound",
        "La categoria selezionata non esiste.");

    public static readonly Error TagDoesNotBelongToCategory = new(
        "Expense.TagDoesNotBelongToCategory",
        "Il tag selezionato non appartiene alla categoria.");
}
