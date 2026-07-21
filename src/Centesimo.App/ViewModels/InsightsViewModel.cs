using System.Collections.ObjectModel;
using System.Globalization;
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
    public ObservableCollection<InsightCategoryViewModel> Categories { get; } = [];
    public ObservableCollection<InsightTrendItemViewModel> Trend { get; } = [];
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
    public bool HasMoreInsights => AllInsights.Count > 3 && !_showAllInsights;
    public string MoreInsightsText => "Mostra tutti gli insight";
    public string TrendDescription => Trend.Count == 0
        ? "Nessun andamento disponibile."
        : $"Andamento delle spese. Picco di {Trend.MaxBy(x => x.AmountCents)!.Amount} il {Trend.MaxBy(x => x.AmountCents)!.Label}.";
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
        Trend.Clear();
        if (result.IsFailure) { Total = FormatMoney(0); Comparison = ""; ErrorMessage = result.Error.Message; } else Apply(result.Value);
        IsLoading = false;
        OnPropertyChanged(nameof(SummaryLabel));
        OnPropertyChanged(nameof(IsMonthly));
        OnPropertyChanged(nameof(HasInsights));
        OnPropertyChanged(nameof(HasCategories));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(TrendDescription));
        OnPropertyChanged(nameof(HasMoreInsights));
    }
    private void Apply(InsightsOverview overview)
    {
        Total = FormatMoney(overview.SpentCents);
        Comparison = overview.ChangePercentage.HasValue ? $"{(overview.ChangePercentage >= 0 ? "+" : "")}{overview.ChangePercentage:P0} rispetto al periodo precedente" : "Continua a registrare le spese per confrontare questo periodo.";
        _showAllInsights = false;
        foreach (var item in overview.Insights)
            AllInsights.Add(new(item.Title, item.Description, item.CategoryId, item.ExpenseId,
                item.ExpenseId.HasValue || (_period == InsightPeriod.Month && item.CategoryId.HasValue)));
        RefreshInsights();
        var maximum = overview.Trend.Max(x => x.SpentCents);
        foreach (var point in overview.Trend)
            Trend.Add(new InsightTrendItemViewModel(point.Label, FormatMoney(point.SpentCents), point.SpentCents,
                maximum == 0 ? 0 : (double)point.SpentCents / maximum));
        foreach (var category in overview.Categories)
            Categories.Add(new(category.CategoryId, category.Name, category.Icon, category.Color,
                FormatMoney(category.SpentCents), $"{category.Percentage:P0} del totale",
                category.ChangePercentage.HasValue ? $"{(category.ChangePercentage >= 0 ? "+" : "")}{category.ChangePercentage:P0} {(category.ChangePercentage >= 0 ? "in più" : "in meno")}" : "Nessun confronto",
                category.Percentage, _period == InsightPeriod.Month));
    }
    private static string FormatMoney(long cents) => (cents / 100m).ToString("C", ItalianCulture);
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
    public sealed record InsightTrendItemViewModel(string Label, string Amount, long AmountCents, double Progress);
    public sealed record InsightCategoryViewModel(Guid CategoryId, string Name, string Icon, string Color,
        string Amount, string Percentage, string Change, double Progress, bool IsActionable)
    {
        public string Description => $"{Name}, {Amount}, {Percentage}, {Change}.";
    }
}
