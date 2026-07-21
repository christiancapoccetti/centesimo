namespace Centesimo.App.Pages;

public partial class LicensePage : ContentPage
{
    public LicensePage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public string LicenseText { get; private set; } = "Caricamento della licenza...";

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (LicenseText != "Caricamento della licenza...")
            return;

        await LoadLicense();
    }

    private async Task LoadLicense()
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync("GPL-3.0.txt");
            using var reader = new StreamReader(stream);
            LicenseText = await reader.ReadToEndAsync();
        }
        catch
        {
            LicenseText = "Non è stato possibile caricare il testo della licenza.";
        }

        OnPropertyChanged(nameof(LicenseText));
    }
}
