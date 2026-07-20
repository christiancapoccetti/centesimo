using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class CategorySpendingPage : ContentPage, IQueryAttributable
{
    private readonly CategorySpendingViewModel _viewModel;

    public CategorySpendingPage(CategorySpendingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("categoryId", out var categoryValue) ||
            !Guid.TryParse(categoryValue?.ToString(), out var categoryId) ||
            !query.TryGetValue("year", out var yearValue) ||
            !int.TryParse(yearValue?.ToString(), out var year) ||
            !query.TryGetValue("month", out var monthValue) ||
            !int.TryParse(monthValue?.ToString(), out var month))
            return;

        _viewModel.Initialize(categoryId, year, month);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnBackClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    private async void OnTagClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CategorySpendingViewModel.TagSpendingItemViewModel tag })
            return;

        var tagId = tag.TagId?.ToString() ?? "none";
        var tagName = Uri.EscapeDataString(tag.Name);
        await Shell.Current.GoToAsync($"{nameof(TagExpensesPage)}?categoryId={_viewModel.CategoryId}" +
            $"&year={_viewModel.Year}&month={_viewModel.Month}&tagId={tagId}&tagName={tagName}");
    }

    private void OnCardPressed(object? sender, EventArgs e) => InteractionFeedback.Press(sender);
    private void OnCardReleased(object? sender, EventArgs e) => InteractionFeedback.Release(sender);
}