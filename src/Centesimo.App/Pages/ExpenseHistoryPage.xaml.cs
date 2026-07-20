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

    private async void OnPreviousMonthClicked(object? sender, EventArgs e) =>
        await _viewModel.PreviousMonth();

    private async void OnNextMonthClicked(object? sender, EventArgs e) =>
        await _viewModel.NextMonth();

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: ExpenseHistoryViewModel.ExpenseHistoryItemViewModel expense })
            return;

        await Shell.Current.GoToAsync($"{nameof(ExpenseEditorPage)}?expenseId={expense.ExpenseId}");
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: ExpenseHistoryViewModel.ExpenseHistoryItemViewModel expense })
            return;

        var confirmed = await DisplayAlertAsync(
            "Elimina spesa",
            $"Vuoi eliminare la spesa di {expense.Amount}? Questa azione non può essere annullata.",
            "Elimina",
            "Annulla");
        if (!confirmed)
            return;

        await _viewModel.Delete(expense.ExpenseId);
    }
}
