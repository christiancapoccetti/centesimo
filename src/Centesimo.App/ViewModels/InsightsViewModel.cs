using System.Collections.ObjectModel;
using System.Globalization;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class InsightsViewModel(IInsightsService service) : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private InsightPeriod _period = InsightPeriod.Month;
    private bool _isLoading;
    private string _errorMessage = "";
    private string _total = FormatMoney(0);
    private string _comparison = "Continua a registrare le spese per confrontare questo periodo.";
    public ObservableCollection<InsightCardViewModel> Insights { get; } = [];
    public ObservableCollection<InsightCategoryViewModel> Categories { get; } = [];
    public ObservableCollection<InsightTrendItemViewModel> Trend { get; } = [];
    public string SummaryLabel => _period == InsightPeriod.Month ? "QUESTO MESE" : "QUESTO ANNO";
    public string Total { get => _total; private set => SetProperty(ref _total, value); }
    public string Comparison { get => _comparison; private set => SetProperty(ref _comparison, value); }
    public bool IsLoading { get => _isLoading; private set => SetProperty(ref _isLoading, value); }
    public string ErrorMessage { get => _errorMessage; private set { if (SetProperty(ref _errorMessage, value)) OnPropertyChanged(nameof(HasError)); } }
    public bool HasError => ErrorMessage.HasValue(); public bool IsEmpty => !IsLoading && !HasError && Categories.Count == 0;
    public bool HasInsights => Insights.Count > 0; public bool HasCategories => Categories.Count > 0; public bool IsMonthly => _period == InsightPeriod.Month;
    public string TrendDescription => Trend.Count == 0
        ? "Nessun andamento disponibile."
        : $"Andamento delle spese: {Trend.Count} punti nel periodo selezionato.";
    public Task Load() => Load(_period); public Task ShowMonth() => Load(InsightPeriod.Month); public Task ShowYear() => Load(InsightPeriod.Year);
    private async Task Load(InsightPeriod period)
    {
        if (IsLoading) return;
        _period = period; IsLoading = true; ErrorMessage = "";
        var result = await service.Get(period);
        Insights.Clear();
        Categories.Clear();
        Trend.Clear();
        if (result.IsFailure) { Total = FormatMoney(0); Comparison = ""; ErrorMessage = result.Error.Message; } else Apply(result.Value);
        IsLoading = false;
        OnPropertyChanged(nameof(SummaryLabel));
        OnPropertyChanged(nameof(IsMonthly));
        OnPropertyChanged(nameof(HasInsights));
        OnPropertyChanged(nameof(HasCategories));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(TrendDescription));
    }
    private void Apply(InsightsOverview overview)
    {
        Total = FormatMoney(overview.SpentCents);
        Comparison = overview.ChangePercentage.HasValue ? $"{(overview.ChangePercentage >= 0 ? "+" : "")}{overview.ChangePercentage:P0} rispetto al periodo precedente" : "Continua a registrare le spese per confrontare questo periodo.";
        foreach (var item in overview.Insights) Insights.Add(new(item.Title, item.Description, item.CategoryId));
        var maximum = overview.Trend.Max(x => x.SpentCents);
        foreach (var point in overview.Trend)
            Trend.Add(new InsightTrendItemViewModel(point.Label, maximum == 0 ? 0 : (double)point.SpentCents / maximum));
        foreach (var category in overview.Categories) Categories.Add(new(category.CategoryId, category.Name, category.Icon, category.Color, FormatMoney(category.SpentCents), $"{category.Percentage:P0} del totale", category.ChangePercentage.HasValue ? $"{(category.ChangePercentage >= 0 ? "+" : "")}{category.ChangePercentage:P0} {(category.ChangePercentage >= 0 ? "in più" : "in meno")}" : "Nessun confronto", category.Percentage));
    }
    private static string FormatMoney(long cents) => (cents / 100m).ToString("C", ItalianCulture);
    public sealed record InsightCardViewModel(string Title, string Description, Guid? CategoryId);
    public sealed record InsightTrendItemViewModel(string Label, double Progress);
    public sealed record InsightCategoryViewModel(Guid CategoryId, string Name, string Icon, string Color, string Amount, string Percentage, string Change, double Progress)
    {
        public string Description => $"{Name}, {Amount}, {Percentage}, {Change}.";
    }
}
