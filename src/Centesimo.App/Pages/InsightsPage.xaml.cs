using Centesimo.App.ViewModels;
using Centesimo.App.Controls;
namespace Centesimo.App.Pages;
public partial class InsightsPage : ContentPage
{
    private readonly InsightsViewModel _viewModel;

    public InsightsPage(InsightsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnMonthClicked(object? sender, EventArgs e) => await _viewModel.ShowMonth();
    private async void OnYearClicked(object? sender, EventArgs e) => await _viewModel.ShowYear();
    private async void OnRetryClicked(object? sender, EventArgs e) => await _viewModel.Load();
    private void OnMoreInsightsClicked(object? sender, EventArgs e) => _viewModel.ShowAllInsights();
    private async void OnAddExpenseClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ExpenseEditorPage));

    private async void OnInsightClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: InsightsViewModel.InsightCardViewModel insight })
            return;
        if (insight.ExpenseId.HasValue)
        {
            await Shell.Current.GoToAsync($"{nameof(ExpenseEditorPage)}?expenseId={insight.ExpenseId}");
            return;
        }
        if (!insight.CategoryId.HasValue || !_viewModel.IsMonthly)
            return;
        await Shell.Current.GoToAsync($"{nameof(CategorySpendingPage)}?categoryId={insight.CategoryId}&year={DateTime.Today.Year}&month={DateTime.Today.Month}");
    }

    private async void OnCategoryClicked(object? sender, CategoryBreakdownItemClickedEventArgs e)
    {
        if (!_viewModel.IsMonthly)
            return;

        await Shell.Current.GoToAsync($"{nameof(CategorySpendingPage)}?categoryId={e.Category.CategoryId}&year={DateTime.Today.Year}&month={DateTime.Today.Month}");
    }
}
