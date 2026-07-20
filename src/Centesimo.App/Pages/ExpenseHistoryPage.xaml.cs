using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class ExpenseHistoryPage : ContentPage
{
    private readonly ExpenseHistoryViewModel _viewModel;

    public ExpenseHistoryPage(ExpenseHistoryViewModel viewModel)
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

    private void OnCardPressed(object? sender, EventArgs e) =>
        InteractionFeedback.Press(sender);

    private void OnCardReleased(object? sender, EventArgs e) =>
        InteractionFeedback.Release(sender);
    private async void OnBackClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
    private async void OnPreviousMonthClicked(object? sender, EventArgs e) =>
        await _viewModel.PreviousMonth();

    private async void OnNextMonthClicked(object? sender, EventArgs e) =>
        await _viewModel.NextMonth();

    private async void OnExpenseTapped(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: ExpenseHistoryViewModel.ExpenseHistoryItemViewModel expense })
            return;

        await Shell.Current.GoToAsync($"{nameof(ExpenseEditorPage)}?expenseId={expense.ExpenseId}");
    }
}
