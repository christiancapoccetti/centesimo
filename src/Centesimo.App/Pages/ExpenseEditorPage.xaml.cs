using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class ExpenseEditorPage : ContentPage
{
    private readonly ExpenseEditorViewModel _viewModel;

    public ExpenseEditorPage(ExpenseEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        viewModel.Saved += OnSaved;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnCategoryChanged(object? sender, EventArgs e) =>
        await _viewModel.LoadTags();

    private async void OnSaved(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
