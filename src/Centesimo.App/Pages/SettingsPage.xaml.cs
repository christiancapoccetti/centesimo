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

            var confirmed = await DisplayAlertAsync(
                "Importa backup",
                "Le spese valide del backup verranno aggiunte ai dati esistenti. Vuoi continuare?",
                "Continua",
                "Annulla");
            if (!confirmed)
                return;

            await using var stream = await file.OpenReadAsync();
            await _viewModel.Import(stream);
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