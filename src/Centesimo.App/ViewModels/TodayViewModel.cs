using System.Collections.ObjectModel;
using System.Globalization;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class TodayViewModel(MonthlyOverviewService overviewService) : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private DateOnly _selectedMonth = CurrentMonth;
    private bool _isLoading;
    private string _errorMessage = "";
    private string _monthlyTotal = FormatMoney(0);
    private string _budgetSummary = "Nessun budget impostato";
    private double _budgetProgress;

    private static DateOnly CurrentMonth => new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public ObservableCollection<MonthlyCategoryItemViewModel> Categories { get; } = [];
    public ObservableCollection<MonthlyExpenseItemViewModel> Expenses { get; } = [];
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public int SelectedYear => _selectedMonth.Year;
    public int SelectedMonth => _selectedMonth.Month;
    public string MonthlyTotal { get => _monthlyTotal; private set => SetProperty(ref _monthlyTotal, value); }
    public string BudgetSummary { get => _budgetSummary; private set => SetProperty(ref _budgetSummary, value); }
    public double BudgetProgress { get => _budgetProgress; private set => SetProperty(ref _budgetProgress, value); }
    public string MonthTitle => _selectedMonth == CurrentMonth
        ? "Questo mese"
        : FormatMonth(_selectedMonth);
    public string ExpenseSectionTitle => _selectedMonth == CurrentMonth
        ? "Ultime spese"
        : $"Ultime spese di {_selectedMonth.ToDateTime(TimeOnly.MinValue).ToString("MMMM", ItalianCulture)}";
    public string EmptyMessage => $"Nessuna spesa in {FormatMonth(_selectedMonth)}.";
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
    public bool HasCategories => Categories.Count > 0;
    public bool HasExpenses => Expenses.Count > 0;
    public bool IsExpenseEmpty => !IsLoading && !HasError && !HasExpenses;
    public bool CanGoPrevious => !IsLoading;
    public bool CanGoNext => !IsLoading && MonthlyNavigation.CanGoNext(_selectedMonth, CurrentMonth);

    public Task Load() => LoadMonth(_selectedMonth);

    public async Task PreviousMonth()
    {
        if (IsLoading)
            return;

        await LoadMonth(MonthlyNavigation.Previous(_selectedMonth));
    }

    public async Task NextMonth()
    {
        if (!CanGoNext)
            return;

        await LoadMonth(MonthlyNavigation.Next(_selectedMonth, CurrentMonth));
    }

    private async Task LoadMonth(DateOnly month)
    {
        if (IsLoading)
            return;

        _selectedMonth = month;
        IsLoading = true;
        ErrorMessage = "";
        NotifyNavigationState();
        var result = await overviewService.Get(month.Year, month.Month);
        Categories.Clear();
        Expenses.Clear();
        if (result.IsFailure)
        {
            MonthlyTotal = FormatMoney(0);
            BudgetSummary = "Nessun budget disponibile";
            BudgetProgress = 0;
            ErrorMessage = result.Error.Message;
        }
        else
            Apply(result.Value);

        IsLoading = false;
        NotifyState();
    }

    private void Apply(MonthlyOverview overview)
    {
        MonthlyTotal = FormatMoney(overview.SpentCents);
        BudgetSummary = overview.TotalBudgetCents.HasValue
            ? $"su {FormatMoney(overview.TotalBudgetCents.Value)} di budget"
            : "Nessun budget impostato";
        BudgetProgress = Progress(overview.SpentCents, overview.TotalBudgetCents);
        foreach (var category in overview.Categories)
            Categories.Add(MonthlyCategoryItemViewModel.From(category));
        foreach (var expense in overview.Expenses)
            Expenses.Add(MonthlyExpenseItemViewModel.From(expense));
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasCategories));
        OnPropertyChanged(nameof(HasExpenses));
        OnPropertyChanged(nameof(IsExpenseEmpty));
        OnPropertyChanged(nameof(MonthTitle));
        OnPropertyChanged(nameof(ExpenseSectionTitle));
        OnPropertyChanged(nameof(EmptyMessage));
        OnPropertyChanged(nameof(SelectedYear));
        OnPropertyChanged(nameof(SelectedMonth));
        NotifyNavigationState();
    }

    private void NotifyNavigationState()
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
    }

    private static double Progress(long spent, long? budget) =>
        budget > 0 ? Math.Min((double)spent / budget.Value, 1) : 0;

    private static string FormatMoney(long cents) =>
        (cents / 100m).ToString("C", ItalianCulture);

    private static string FormatMonth(DateOnly month) =>
        month.ToDateTime(TimeOnly.MinValue).ToString("MMMM yyyy", ItalianCulture);

    public sealed record MonthlyCategoryItemViewModel(
        Guid CategoryId, string Name, string Icon, string Color, string AmountSummary, double Progress,
        bool IsOverBudget, string CardColor, string StatusLabel)
    {
        public static MonthlyCategoryItemViewModel From(MonthlyCategoryOverview category) => new(
            category.CategoryId,
            category.Name,
            category.Icon,
            category.Color,
            category.BudgetCents.HasValue
                ? $"{FormatMoney(category.SpentCents)} su {FormatMoney(category.BudgetCents.Value)}"
                : $"{FormatMoney(category.SpentCents)} · Nessun budget",
            TodayViewModel.Progress(category.SpentCents, category.BudgetCents),
            category.BudgetCents.HasValue && category.SpentCents > category.BudgetCents.Value,
            category.BudgetCents.HasValue && category.SpentCents > category.BudgetCents.Value
                ? "#FFE8E6"
                : "#FFFFFF",
            category.BudgetCents.HasValue && category.SpentCents > category.BudgetCents.Value
                ? "Budget superato"
                : "");
    }

    public sealed record MonthlyExpenseItemViewModel(
        Guid ExpenseId, string CategoryName, string Icon, string Color, string Amount,
        string Date, string Note, bool HasNote)
    {
        public static MonthlyExpenseItemViewModel From(MonthlyExpenseOverview expense) => new(
            expense.ExpenseId,
            expense.CategoryName,
            expense.CategoryIcon,
            expense.CategoryColor,
            FormatMoney(expense.AmountCents),
            expense.OccurredOn.ToDateTime(TimeOnly.MinValue).ToString("ddd d MMM", ItalianCulture),
            expense.Note,
            expense.Note.HasValue());
    }
}
