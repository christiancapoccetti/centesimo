using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _viewModel;
    private readonly CategoryEditorPage _editorPage;

    public CategoriesPage(CategoriesViewModel viewModel, CategoryEditorPage editorPage)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _editorPage = editorPage;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnNewCategoryClicked(object? sender, EventArgs e)
    {
        _editorPage.OpenNew();
        await Navigation.PushModalAsync(_editorPage, false);
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CategoryItemViewModel category })
            return;

        _editorPage.OpenEdit(category);
        await Navigation.PushModalAsync(_editorPage, false);
    }

    private void OnCardPressed(object? sender, EventArgs e) =>
        InteractionFeedback.Press(sender);

    private void OnCardReleased(object? sender, EventArgs e) =>
        InteractionFeedback.Release(sender);

    private async void OnArchiveClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CategoryItemViewModel category })
            return;

        var confirmed = await DisplayAlertAsync(
            "Archivia categoria",
            $"Vuoi archiviare {category.Name}? Le spese esistenti resteranno disponibili.",
            "Archivia",
            "Annulla");
        if (!confirmed)
            return;

        await _viewModel.Archive(category.CategoryId);
    }
}