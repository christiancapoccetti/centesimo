namespace Centesimo.App.Pages;

public partial class OpenSourceLicensesPage : ContentPage
{
    public OpenSourceLicensesPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public string Notices { get; private set; } = "Caricamento delle licenze...";

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (Notices != "Caricamento delle licenze...")
            return;

        await LoadNotices();
    }

    private async Task LoadNotices()
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync("THIRD_PARTY_NOTICES.txt");
            using var reader = new StreamReader(stream);
            Notices = await reader.ReadToEndAsync();
        }
        catch
        {
            Notices = "Non è stato possibile caricare gli avvisi di licenza.";
        }

        OnPropertyChanged(nameof(Notices));
    }
}
