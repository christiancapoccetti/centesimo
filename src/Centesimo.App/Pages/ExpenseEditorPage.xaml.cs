using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class ExpenseEditorPage : ContentPage, IQueryAttributable
{
    private readonly ExpenseEditorViewModel _viewModel;
    private Guid? _expenseId;

    public ExpenseEditorPage(ExpenseEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        viewModel.Saved += OnSaved;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _expenseId = query.TryGetValue("expenseId", out var value)
            && Guid.TryParse(value?.ToString(), out var expenseId)
                ? expenseId
                : null;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load(_expenseId);
    }

    private async void OnCategoryChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_viewModel.IsLoading)
            return;

        await _viewModel.LoadTags();
    }

    private async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject { BindingContext: ExpenseEditorViewModel.CategoryOption category })
            return;

        _viewModel.SelectedCategory = category;
        await _viewModel.LoadTags();
    }

    private async void OnCategorySelectorTapped(object? sender, object category)
    {
        if (category is not ExpenseEditorViewModel.CategoryOption selected)
            return;

        _viewModel.SelectedCategory = selected;
        await _viewModel.LoadTags();
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Elimina spesa",
            "Vuoi eliminare questa spesa? L'azione non può essere annullata.",
            "Elimina",
            "Annulla");
        if (!confirmed)
            return;

        await _viewModel.Delete();
    }
    private async void OnSaved(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
