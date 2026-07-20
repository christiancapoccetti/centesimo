using System.Collections.ObjectModel;
using System.Globalization;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class ExpenseHistoryViewModel(
    ExpenseHistoryService historyService,
    ExpenseService expenseService) : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private DateOnly _selectedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private bool _isLoading;
    private string _errorMessage = "";
    private string _monthTitle = "";

    public ObservableCollection<ExpenseHistoryItemViewModel> Expenses { get; } = [];
    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (!SetProperty(ref _isLoading, value))
                return;

            OnPropertyChanged(nameof(CanNavigatePrevious));
            OnPropertyChanged(nameof(CanGoNext));
        }
    }
    public string MonthTitle { get => _monthTitle; private set => SetProperty(ref _monthTitle, value); }
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
    public bool HasExpenses => Expenses.Count > 0;
    public bool IsEmpty => !IsLoading && !HasError && !HasExpenses;
    public bool CanNavigatePrevious => !IsLoading;
    public bool CanGoNext => !IsLoading
        && _selectedMonth < new DateOnly(DateTime.Today.Year, DateTime.Today.Month, 1);

    public Task Load() => LoadMonth(_selectedMonth);

    public Task PreviousMonth() => LoadMonth(_selectedMonth.AddMonths(-1));

    public async Task NextMonth()
    {
        if (!CanGoNext)
            return;

        await LoadMonth(_selectedMonth.AddMonths(1));
    }

    public async Task<Result> Delete(Guid expenseId)
    {
        var result = await expenseService.Delete(expenseId);
        if (result.IsSuccess)
            await Load();
        else
            ErrorMessage = result.Error.Message;

        return result;
    }

    private async Task LoadMonth(DateOnly month)
    {
        _selectedMonth = month;
        MonthTitle = month.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", ItalianCulture);
        IsLoading = true;
        ErrorMessage = "";
        var result = await historyService.GetMonth(month.Year, month.Month);
        Expenses.Clear();
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            foreach (var expense in result.Value)
                Expenses.Add(ExpenseHistoryItemViewModel.From(expense));

        IsLoading = false;
        NotifyState();
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasExpenses));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(CanGoNext));
    }

    public sealed record ExpenseHistoryItemViewModel(
        Guid ExpenseId,
        string CategoryName,
        string Icon,
        string Color,
        string Amount,
        string Date,
        string Note,
        bool HasNote)
    {
        public static ExpenseHistoryItemViewModel From(ExpenseHistoryItem expense) => new(
            expense.ExpenseId,
            expense.CategoryName,
            expense.CategoryIcon,
            expense.CategoryColor,
            (expense.AmountCents / 100m).ToString("C", ItalianCulture),
            expense.OccurredOn.ToDateTime(TimeOnly.MinValue).ToString("ddd d MMM", ItalianCulture),
            expense.Note,
            expense.Note.HasValue());
    }
}
