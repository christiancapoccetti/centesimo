using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class CategoryEditorPage : ContentPage
{
    private readonly CategoryEditorViewModel _viewModel;

    public CategoryEditorPage(CategoryEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
        viewModel.Saved += OnSaved;
    }

    public void OpenNew() => _viewModel.OpenNew();

    public void OpenEdit(CategoryItemViewModel category) => _viewModel.OpenEdit(category);

    private async void OnSaved(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync();

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync();
}
