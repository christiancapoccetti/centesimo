using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class TagExpensesPage : ContentPage, IQueryAttributable
{
    private readonly TagExpensesViewModel _viewModel;

    public TagExpensesPage(TagExpensesViewModel viewModel)
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
            !int.TryParse(monthValue?.ToString(), out var month) ||
            !query.TryGetValue("tagName", out var tagNameValue))
            return;

        query.TryGetValue("tagId", out var tagValue);
        var tagId = Guid.TryParse(tagValue?.ToString(), out var parsedTagId) ? parsedTagId : (Guid?)null;
        _viewModel.Initialize(categoryId, tagId, tagNameValue?.ToString() ?? "Senza tag", year, month);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnExpenseClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: TagExpensesViewModel.TagExpenseItemViewModel expense })
            return;

        await Shell.Current.GoToAsync($"{nameof(ExpenseEditorPage)}?expenseId={expense.ExpenseId}");
    }

    private void OnCardPressed(object? sender, EventArgs e) => InteractionFeedback.Press(sender);
    private void OnCardReleased(object? sender, EventArgs e) => InteractionFeedback.Release(sender);
}