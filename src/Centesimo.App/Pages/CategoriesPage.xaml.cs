using Centesimo.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.Pages;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _viewModel;
    private readonly IServiceProvider _serviceProvider;

    public CategoriesPage(CategoriesViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _serviceProvider = serviceProvider;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnNewCategoryClicked(object? sender, EventArgs e)
    {
        var editorPage = _serviceProvider.GetRequiredService<CategoryEditorPage>();
        editorPage.OpenNew();
        await Navigation.PushModalAsync(editorPage);
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: CategoryItemViewModel category })
            return;

        var editorPage = _serviceProvider.GetRequiredService<CategoryEditorPage>();
        editorPage.OpenEdit(category);
        await Navigation.PushModalAsync(editorPage);
    }

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
