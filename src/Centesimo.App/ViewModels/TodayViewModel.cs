using System.Collections.ObjectModel;
using System.Globalization;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class TodayViewModel(TodayOverviewService overviewService) : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private bool _isLoading;
    private string _errorMessage = "";
    private string _monthlyTotal = FormatMoney(0);
    private string _budgetSummary = "Nessun budget impostato";
    private double _budgetProgress;

    public ObservableCollection<TodayCategoryItemViewModel> Categories { get; } = [];
    public ObservableCollection<TodayExpenseItemViewModel> Expenses { get; } = [];
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public string MonthlyTotal { get => _monthlyTotal; private set => SetProperty(ref _monthlyTotal, value); }
    public string BudgetSummary { get => _budgetSummary; private set => SetProperty(ref _budgetSummary, value); }
    public double BudgetProgress { get => _budgetProgress; private set => SetProperty(ref _budgetProgress, value); }
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

    public async Task Load()
    {
        IsLoading = true;
        ErrorMessage = "";
        var result = await overviewService.Get(DateOnly.FromDateTime(DateTime.Today));
        Categories.Clear();
        Expenses.Clear();
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            Apply(result.Value);

        IsLoading = false;
        NotifyState();
    }

    private void Apply(TodayOverview overview)
    {
        MonthlyTotal = FormatMoney(overview.MonthlySpentCents);
        BudgetSummary = overview.TotalBudgetCents.HasValue
            ? $"su {FormatMoney(overview.TotalBudgetCents.Value)} di budget"
            : "Nessun budget impostato";
        BudgetProgress = Progress(overview.MonthlySpentCents, overview.TotalBudgetCents);
        foreach (var category in overview.Categories)
            Categories.Add(TodayCategoryItemViewModel.From(category));
        foreach (var expense in overview.Expenses)
            Expenses.Add(TodayExpenseItemViewModel.From(expense));
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasCategories));
        OnPropertyChanged(nameof(HasExpenses));
        OnPropertyChanged(nameof(IsExpenseEmpty));
    }

    private static double Progress(long spent, long? budget) =>
        budget > 0 ? Math.Min((double)spent / budget.Value, 1) : 0;

    private static string FormatMoney(long cents) =>
        (cents / 100m).ToString("C", ItalianCulture);

    public sealed record TodayCategoryItemViewModel(
        string Name, string Icon, string Color, string AmountSummary, double Progress)
    {
        public static TodayCategoryItemViewModel From(TodayCategoryOverview category) => new(
            category.Name,
            category.Icon,
            category.Color,
            category.BudgetCents.HasValue
                ? $"{FormatMoney(category.SpentCents)} su {FormatMoney(category.BudgetCents.Value)}"
                : $"{FormatMoney(category.SpentCents)} · Nessun budget",
            TodayViewModel.Progress(category.SpentCents, category.BudgetCents));
    }

    public sealed record TodayExpenseItemViewModel(
        string CategoryName, string Icon, string Color, string Amount, string Note, bool HasNote)
    {
        public static TodayExpenseItemViewModel From(TodayExpenseOverview expense) => new(
            expense.CategoryName,
            expense.CategoryIcon,
            expense.CategoryColor,
            FormatMoney(expense.AmountCents),
            expense.Note,
            expense.Note.HasValue());
    }
}
