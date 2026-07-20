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

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTags();
    }

    private async void OnArchiveTagClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CategoryTagItemViewModel tag })
            return;

        var confirmed = await DisplayAlertAsync(
            "Rimuovi tag",
            $"Vuoi rimuovere il tag {tag.Name}? Le spese esistenti resteranno disponibili.",
            "Rimuovi",
            "Annulla");
        if (!confirmed)
            return;

        await _viewModel.ArchiveTag(tag.TagId);
    }

    private async void OnSaved(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync();

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await Navigation.PopModalAsync();
}
