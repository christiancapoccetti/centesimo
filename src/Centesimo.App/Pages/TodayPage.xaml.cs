using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _viewModel;

    public TodayPage(TodayViewModel viewModel)
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

    private async void OnExpenseHistoryClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ExpenseHistoryPage));

    private async void OnTodayExpenseTapped(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: TodayViewModel.TodayExpenseItemViewModel expense })
            return;

        await Shell.Current.GoToAsync($"{nameof(ExpenseEditorPage)}?expenseId={expense.ExpenseId}");
    }
    private void OnCardPressed(object? sender, EventArgs e) =>
        InteractionFeedback.Press(sender);

    private void OnCardReleased(object? sender, EventArgs e) =>
        InteractionFeedback.Release(sender);
    private async void OnAddExpenseClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ExpenseEditorPage));
}
