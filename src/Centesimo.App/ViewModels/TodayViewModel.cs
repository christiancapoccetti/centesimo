using System.Collections.ObjectModel;
using System.Globalization;
using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.ViewModels;

public sealed class TodayViewModel : ObservableObject
{
    private readonly IServiceScopeFactory _scopeFactory;
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private DateOnly _selectedMonth = CurrentMonth;
    private bool _isYearlyOverview;
    private bool _isLoading;
    private string _errorMessage = "";
    private string _monthlyTotal = FormatMoney(0);
    private string _budgetSummary = "Nessun budget impostato";
    private double _budgetProgress;
    private bool _isSpeechSheetVisible;
    private bool _isSpeechListening;
    private bool _isSpeechProcessing;
    private bool _isSpeechPreparing;
    private bool _isSpeechReady;
    private bool _isSpeechPreparationFailed;
    private string _speechAvailabilityMessage = "Preparazione del riconoscimento vocale…";
    private string _speechPreparationActionText = "Riprova";
    private string _speechTranscription = "Parla ora, poi premi Ferma registrazione.";
    private string _speechErrorMessage = "";

    public TodayViewModel(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        ToggleOverviewCommand = new AsyncCommand(ToggleOverview);
    }

    private static DateOnly CurrentMonth => new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public ObservableCollection<MonthlyCategoryItemViewModel> Categories { get; } = [];
    public ObservableCollection<MonthlyExpenseItemViewModel> Expenses { get; } = [];
    public AsyncCommand ToggleOverviewCommand { get; }
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public int SelectedYear => _selectedMonth.Year;
    public int SelectedMonth => _selectedMonth.Month;
    public bool IsYearlyOverview { get => _isYearlyOverview; private set => SetProperty(ref _isYearlyOverview, value); }
    public string MonthlyTotal { get => _monthlyTotal; private set => SetProperty(ref _monthlyTotal, value); }
    public string BudgetSummary { get => _budgetSummary; private set => SetProperty(ref _budgetSummary, value); }
    public double BudgetProgress { get => _budgetProgress; private set => SetProperty(ref _budgetProgress, value); }
    public string MonthTitle => IsYearlyOverview
        ? _selectedMonth.Year == DateTime.Today.Year ? "Questo anno" : _selectedMonth.Year.ToString()
        : _selectedMonth == CurrentMonth
        ? "Questo mese"
        : FormatMonth(_selectedMonth);
    public string SummaryLabel => IsYearlyOverview
        ? _selectedMonth.Year == DateTime.Today.Year ? "QUESTO ANNO" : $"ANNO {_selectedMonth.Year}"
        : "QUESTO MESE";
    public string HomeSubtitle => IsYearlyOverview
        ? "Ecco come sta andando il tuo anno."
        : "Ecco come sta andando il tuo mese.";
    public bool IsExpenseSectionVisible => true;
    public string ExpenseSectionTitle => IsYearlyOverview
        ? "Le spese più alte dell'anno"
        : _selectedMonth == CurrentMonth
        ? "Ultime spese"
        : $"Ultime spese di {_selectedMonth.ToDateTime(TimeOnly.MinValue).ToString("MMMM", ItalianCulture)}";
    public string EmptyMessage => IsYearlyOverview
        ? $"Nessuna spesa nel {_selectedMonth.Year}."
        : $"Nessuna spesa in {FormatMonth(_selectedMonth)}.";
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
    public bool CanGoNext => !IsLoading && (IsYearlyOverview
        ? _selectedMonth.Year < DateTime.Today.Year
        : MonthlyNavigation.CanGoNext(_selectedMonth, CurrentMonth));
    public bool IsSpeechSheetVisible { get => _isSpeechSheetVisible; set => SetProperty(ref _isSpeechSheetVisible, value); }
    public bool IsSpeechListening { get => _isSpeechListening; set => SetProperty(ref _isSpeechListening, value); }
    public bool IsSpeechProcessing { get => _isSpeechProcessing; set => SetProperty(ref _isSpeechProcessing, value); }
    public bool IsSpeechPreparing { get => _isSpeechPreparing; set => SetProperty(ref _isSpeechPreparing, value); }
    public bool IsSpeechReady { get => _isSpeechReady; set => SetProperty(ref _isSpeechReady, value); }
    public bool IsSpeechPreparationFailed { get => _isSpeechPreparationFailed; set => SetProperty(ref _isSpeechPreparationFailed, value); }
    public string SpeechAvailabilityMessage { get => _speechAvailabilityMessage; set => SetProperty(ref _speechAvailabilityMessage, value); }
    public string SpeechPreparationActionText { get => _speechPreparationActionText; set => SetProperty(ref _speechPreparationActionText, value); }
    public string SpeechTranscription { get => _speechTranscription; set => SetProperty(ref _speechTranscription, value); }
    public string SpeechErrorMessage { get => _speechErrorMessage; set { if (SetProperty(ref _speechErrorMessage, value)) OnPropertyChanged(nameof(HasSpeechError)); } }
    public bool HasSpeechError => SpeechErrorMessage.HasValue();

    public Task Load() => IsYearlyOverview ? LoadYear(_selectedMonth) : LoadMonth(_selectedMonth);

    public async Task ToggleOverview()
    {
        if (IsLoading)
            return;

        IsYearlyOverview = !IsYearlyOverview;
        if (IsYearlyOverview)
            await LoadYear(_selectedMonth);
        else
            await LoadMonth(_selectedMonth);
    }

    public async Task PreviousMonth()
    {
        if (IsLoading)
            return;

        if (IsYearlyOverview)
        {
            await LoadYear(_selectedMonth.AddYears(-1));
            return;
        }

        await LoadMonth(MonthlyNavigation.Previous(_selectedMonth));
    }

    public async Task NextMonth()
    {
        if (!CanGoNext)
            return;

        if (IsYearlyOverview)
        {
            await LoadYear(_selectedMonth.AddYears(1));
            return;
        }

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
        Result<MonthlyOverview> result;
        using (var scope = _scopeFactory.CreateScope())
        {
            var overviewService = scope.ServiceProvider.GetRequiredService<MonthlyOverviewService>();
            result = await overviewService.Get(month.Year, month.Month);
        }
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

    private async Task LoadYear(DateOnly year)
    {
        if (IsLoading)
            return;

        _selectedMonth = year;
        IsLoading = true;
        ErrorMessage = "";
        NotifyNavigationState();
        Result<YearlyOverview> result;
        using (var scope = _scopeFactory.CreateScope())
        {
            var overviewService = scope.ServiceProvider.GetRequiredService<YearlyOverviewService>();
            result = await overviewService.Get(year.Year);
        }
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

    private void Apply(YearlyOverview overview)
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
        OnPropertyChanged(nameof(SummaryLabel));
        OnPropertyChanged(nameof(HomeSubtitle));
        OnPropertyChanged(nameof(IsExpenseSectionVisible));
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

        public static MonthlyCategoryItemViewModel From(YearlyCategoryOverview category) => new(
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

        public static MonthlyExpenseItemViewModel From(YearlyExpenseOverview expense) => new(
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
