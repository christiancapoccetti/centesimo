using System.Collections.ObjectModel;
using System.Globalization;
using Centesimo.App.Controls;
using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.ViewModels;

public sealed class InsightsViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private InsightPeriod _period = InsightPeriod.Month;
    private bool _isLoading;
    private string _errorMessage = "";
    private string _total = FormatMoney(0);
    private string _comparison = "Continua a registrare le spese per confrontare questo periodo.";
    private bool _showAllInsights;
    public ObservableCollection<InsightCardViewModel> Insights { get; } = [];
    public ObservableCollection<InsightCardViewModel> AllInsights { get; } = [];
    public ObservableCollection<CategoryBreakdownItemViewModel> Categories { get; } = [];
    public string SummaryLabel => _period == InsightPeriod.Month ? "QUESTO MESE" : "QUESTO ANNO";
    public string Total { get => _total; private set => SetProperty(ref _total, value); }
    public string Comparison { get => _comparison; private set => SetProperty(ref _comparison, value); }
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public string ErrorMessage { get => _errorMessage; private set { if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => ErrorMessage.HasValue();
    public bool IsEmpty => !IsLoading && !HasError && Categories.Count == 0;
    public bool HasInsights => Insights.Count > 0;
    public bool HasCategories => Categories.Count > 0;
    public bool IsMonthly => _period == InsightPeriod.Month;
    public string MonthSelectorBackground => IsMonthly ? "#DDF4EB" : "Transparent";
    public string YearSelectorBackground => IsMonthly ? "Transparent" : "#DDF4EB";
    public string MonthSelectorDescription => IsMonthly ? "Questo mese, selezionato" : "Questo mese, non selezionato";
    public string YearSelectorDescription => IsMonthly ? "Questo anno, non selezionato" : "Questo anno, selezionato";
    public bool HasMoreInsights => AllInsights.Count > 3 && !_showAllInsights;
    public string MoreInsightsText => "Mostra tutti gli insight";
    public double ComparisonProgress { get; private set; }
    public bool HasComparison => ComparisonProgress > 0;
    public string ComparisonDescription => Comparison.HasValue()
        ? $"Confronto: {Comparison}"
        : "Nessun confronto disponibile.";
    public Task Load() => Load(_period); public Task ShowMonth() => Load(InsightPeriod.Month); public Task ShowYear() => Load(InsightPeriod.Year);
    public void ShowAllInsights()
    {
        _showAllInsights = true;
        RefreshInsights();
    }
    private async Task Load(InsightPeriod period)
    {
        if (IsLoading) return;
        _period = period; IsLoading = true; ErrorMessage = "";
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IInsightsService>();
        var result = await service.Get(period);
        Insights.Clear();
        AllInsights.Clear();
        Categories.Clear();
        if (result.IsFailure)
        {
            Total = FormatMoney(0);
            Comparison = "";
            ComparisonProgress = 0;
            ErrorMessage = result.Error.Message;
        }
        else
            Apply(result.Value);
        IsLoading = false;
        OnPropertyChanged(nameof(SummaryLabel));
        OnPropertyChanged(nameof(IsMonthly));
        OnPropertyChanged(nameof(MonthSelectorBackground));
        OnPropertyChanged(nameof(YearSelectorBackground));
        OnPropertyChanged(nameof(MonthSelectorDescription));
        OnPropertyChanged(nameof(YearSelectorDescription));
        OnPropertyChanged(nameof(HasInsights));
        OnPropertyChanged(nameof(HasCategories));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ComparisonProgress));
        OnPropertyChanged(nameof(HasComparison));
        OnPropertyChanged(nameof(ComparisonDescription));
        OnPropertyChanged(nameof(HasMoreInsights));
    }
    private void Apply(InsightsOverview overview)
    {
        Total = FormatMoney(overview.SpentCents);
        Comparison = FormatComparison(overview);
        ComparisonProgress = overview.ComparedSpentCents.HasValue
            ? (double)overview.SpentCents / Math.Max(overview.SpentCents, overview.ComparedSpentCents.Value)
            : 0;
        _showAllInsights = false;
        foreach (var item in overview.Insights)
            AllInsights.Add(new(item.Title, item.Description, item.CategoryId, item.ExpenseId,
                item.ExpenseId.HasValue || (_period == InsightPeriod.Month && item.CategoryId.HasValue)));
        RefreshInsights();
        foreach (var category in overview.Categories)
            Categories.Add(new(category.CategoryId, category.Name, category.Icon, category.Color,
                FormatMoney(category.SpentCents), $"{category.Percentage:P0} del totale",
                category.ChangePercentage.HasValue ? $"{(category.ChangePercentage >= 0 ? "+" : "")}{category.ChangePercentage:P0} {(category.ChangePercentage >= 0 ? "in più" : "in meno")}" : "Nessun confronto",
                category.Percentage, _period == InsightPeriod.Month));
    }
    private static string FormatMoney(long cents) => (cents / 100m).ToString("C", ItalianCulture);
    private static string FormatComparison(InsightsOverview overview)
    {
        if (!overview.ChangePercentage.HasValue)
            return "Nessun periodo equivalente da confrontare.";

        var current = overview.To.ToDateTime(TimeOnly.MinValue);
        var change = $"{(overview.ChangePercentage >= 0 ? "+" : "")}{overview.ChangePercentage:P0}";
        if (overview.Period == InsightPeriod.Month)
            return $"{current.ToString("MMMM", ItalianCulture)} rispetto a {current.AddMonths(-1).ToString("MMMM", ItalianCulture)}: {change}";

        return $"gennaio–{current.ToString("MMMM", ItalianCulture)} {current.Year} rispetto allo stesso periodo del {current.Year - 1}: {change}";
    }
    private void RefreshInsights()
    {
        Insights.Clear();
        foreach (var item in _showAllInsights ? AllInsights : AllInsights.Take(3))
            Insights.Add(item);
        OnPropertyChanged(nameof(HasInsights));
        OnPropertyChanged(nameof(HasMoreInsights));
    }

    public sealed record InsightCardViewModel(string Title, string Description, Guid? CategoryId, Guid? ExpenseId,
        bool IsActionable);
}
