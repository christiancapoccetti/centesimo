using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private async void OnImportClicked(object? sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Scegli il backup Money Manager"
            });
            if (file is null)
                return;

            await using var stream = await file.OpenReadAsync();
            var preview = await _viewModel.Preview(stream);
            if (preview.IsFailure)
                return;

            var confirmed = await DisplayAlertAsync(
                "Importa backup",
                $"Verranno importate {preview.Value.CategoriesCount} categorie, " +
                $"{preview.Value.TagsCount} tag e {preview.Value.ExpensesCount} spese. Vuoi continuare?",
                "Importa",
                "Annulla");
            if (!confirmed)
                return;

            await _viewModel.Import(preview.Value);
        }
        catch (OperationCanceledException)
        {
            _viewModel.ShowError("Importazione annullata.");
        }
        catch
        {
            _viewModel.ShowError("Non è stato possibile aprire il backup selezionato.");
        }
    }
}
