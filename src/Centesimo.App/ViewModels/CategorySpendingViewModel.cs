using System.Collections.ObjectModel;
using System.Globalization;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class CategorySpendingViewModel(CategorySpendingService spendingService) : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private Guid _categoryId;
    private int _year;
    private int _month;
    private CategorySpendingPeriod _period;
    private bool _isLoading;
    private string _title = "Dettaglio categoria";
    private string _monthTitle = "";
    private string _total = FormatMoney(0);
    private string _categoryIcon = "more";
    private string _categoryColor = "#6F7975";
    private string _errorMessage = "";
    private string _budgetSummary = "Nessun budget impostato";
    private double _budgetProgress;
    private bool _isOverBudget;

    public Guid CategoryId => _categoryId;
    public int Year => _year;
    public int Month => _month;
    public bool IsYearly => _period == CategorySpendingPeriod.Year;
    public ObservableCollection<TagSpendingItemViewModel> Tags { get; } = [];
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public string Title { get => _title; private set => SetProperty(ref _title, value); }
    public string MonthTitle { get => _monthTitle; private set => SetProperty(ref _monthTitle, value); }
    public string Total { get => _total; private set => SetProperty(ref _total, value); }
    public string CategoryIcon { get => _categoryIcon; private set => SetProperty(ref _categoryIcon, value); }
    public string CategoryColor { get => _categoryColor; private set => SetProperty(ref _categoryColor, value); }
    public string BudgetSummary { get => _budgetSummary; private set => SetProperty(ref _budgetSummary, value); }
    public double BudgetProgress { get => _budgetProgress; private set => SetProperty(ref _budgetProgress, value); }
    public bool IsOverBudget { get => _isOverBudget; private set => SetProperty(ref _isOverBudget, value); }
    public string BudgetStatus => IsOverBudget ? "Budget superato" : "Budget rispettato";
    public string BudgetStatusColor => IsOverBudget ? "#BA1A1A" : "#196D61";
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
    public bool HasTags => Tags.Count > 0;
    public bool IsEmpty => !IsLoading && !HasError && !HasTags;

    public void Initialize(Guid categoryId, int year, int month, CategorySpendingPeriod period = CategorySpendingPeriod.Month)
    {
        _categoryId = categoryId;
        _year = year;
        _month = month;
        _period = period;
    }

    public async Task Load()
    {
        if (IsLoading || _categoryId == Guid.Empty)
            return;

        IsLoading = true;
        ErrorMessage = "";
        Tags.Clear();
        var result = await spendingService.Get(_categoryId, _year, _month, _period);
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            Apply(result.Value);

        IsLoading = false;
        NotifyState();
    }

    private void Apply(CategorySpendingOverview overview)
    {
        Title = overview.CategoryName;
        CategoryIcon = overview.CategoryIcon;
        CategoryColor = overview.CategoryColor;
        Total = FormatMoney(overview.SpentCents);
        BudgetSummary = overview.BudgetCents.HasValue
            ? $"{FormatMoney(overview.SpentCents)} su {FormatMoney(overview.BudgetCents.Value)} di budget"
            : "Nessun budget impostato";
        BudgetProgress = Progress(overview.SpentCents, overview.BudgetCents);
        IsOverBudget = overview.BudgetCents.HasValue && overview.SpentCents > overview.BudgetCents.Value;
        MonthTitle = overview.Period == CategorySpendingPeriod.Year
            ? $"Anno {overview.Year}"
            : new DateOnly(overview.Year, overview.Month, 1)
                .ToDateTime(TimeOnly.MinValue)
                .ToString("MMMM yyyy", ItalianCulture);
        foreach (var tag in overview.Tags)
            Tags.Add(TagSpendingItemViewModel.From(tag, overview.Period == CategorySpendingPeriod.Month));
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasTags));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(BudgetStatus));
        OnPropertyChanged(nameof(BudgetStatusColor));
    }

    private static string FormatMoney(long cents) => (cents / 100m).ToString("C", ItalianCulture);

    private static double Progress(long spent, long? budget) =>
        budget > 0 ? Math.Min((double)spent / budget.Value, 1) : 0;

    public sealed record TagSpendingItemViewModel(Guid? TagId, string Name, string Total, string CountText, bool IsActionable)
    {
        public static TagSpendingItemViewModel From(TagSpendingOverview tag, bool isActionable) => new(
            tag.TagId,
            tag.Name,
            FormatMoney(tag.SpentCents),
            tag.Expenses.Count == 1 ? "1 spesa" : $"{tag.Expenses.Count} spese",
            isActionable);
    }
}
