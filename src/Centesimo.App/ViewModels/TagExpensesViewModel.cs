using System.Collections.ObjectModel;
using System.Globalization;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class TagExpensesViewModel(CategorySpendingService spendingService) : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private Guid _categoryId;
    private Guid? _tagId;
    private int _year;
    private int _month;
    private bool _isLoading;
    private string _title = "Spese del tag";
    private string _monthTitle = "";
    private string _total = FormatMoney(0);
    private string _errorMessage = "";

    public ObservableCollection<TagExpenseItemViewModel> Expenses { get; } = [];
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public string Title { get => _title; private set => SetProperty(ref _title, value); }
    public string MonthTitle { get => _monthTitle; private set => SetProperty(ref _monthTitle, value); }
    public string Total { get => _total; private set => SetProperty(ref _total, value); }
    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }
    public bool HasError => ErrorMessage.HasValue();
    public bool IsEmpty => !IsLoading && !HasError && Expenses.Count == 0;

    public void Initialize(Guid categoryId, Guid? tagId, string tagName, int year, int month)
    {
        _categoryId = categoryId;
        _tagId = tagId;
        _year = year;
        _month = month;
        Title = tagName;
    }

    public async Task Load()
    {
        if (IsLoading || _categoryId == Guid.Empty)
            return;

        IsLoading = true;
        ErrorMessage = "";
        Expenses.Clear();
        var result = await spendingService.Get(_categoryId, _year, _month);
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
        {
            var tag = result.Value.Tags.SingleOrDefault(item => item.TagId == _tagId);
            Total = FormatMoney(tag?.SpentCents ?? 0);
            MonthTitle = new DateOnly(_year, _month, 1).ToDateTime(TimeOnly.MinValue)
                .ToString("MMMM yyyy", ItalianCulture);
            if (tag is not null)
                foreach (var expense in tag.Expenses)
                    Expenses.Add(TagExpenseItemViewModel.From(expense));
        }

        IsLoading = false;
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private static string FormatMoney(long cents) => (cents / 100m).ToString("C", ItalianCulture);

    public sealed record TagExpenseItemViewModel(Guid ExpenseId, string Amount, string Date, string Note, bool HasNote)
    {
        public static TagExpenseItemViewModel From(CategoryExpenseOverview expense) => new(
            expense.ExpenseId,
            FormatMoney(expense.AmountCents),
            expense.OccurredOn.ToDateTime(TimeOnly.MinValue).ToString("ddd d MMM", ItalianCulture),
            expense.Note,
            expense.Note.HasValue());
    }
}