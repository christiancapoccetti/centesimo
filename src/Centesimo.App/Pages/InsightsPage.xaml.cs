using Centesimo.App.ViewModels;
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

    private async void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: InsightsViewModel.InsightCategoryViewModel category } || !_viewModel.IsMonthly)
            return;

        await Shell.Current.GoToAsync($"{nameof(CategorySpendingPage)}?categoryId={category.CategoryId}&year={DateTime.Today.Year}&month={DateTime.Today.Month}");
    }
}
