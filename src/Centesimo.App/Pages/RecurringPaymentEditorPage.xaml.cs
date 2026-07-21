using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class RecurringPaymentEditorPage : ContentPage, IQueryAttributable
{
    private readonly RecurringPaymentEditorViewModel _viewModel;
    private readonly RecurringPaymentAutomation _automation;
    private Guid? _paymentId;
    public RecurringPaymentEditorPage(RecurringPaymentEditorViewModel viewModel,
        RecurringPaymentAutomation automation)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _automation = automation;
        BindingContext = viewModel;
        viewModel.Saved += OnSaved;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _paymentId = query.TryGetValue("paymentId", out var value) &&
            Guid.TryParse(value?.ToString(), out var id)
                ? id
                : null;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load(_paymentId);
    }
    private async void OnCategorySelectorTapped(object? sender, object category)
    {
        if (category is not ExpenseEditorViewModel.CategoryOption option)
            return;

        _viewModel.SelectedCategory = option;
        await _viewModel.LoadTags();
    }
    private async void OnCloseClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
    private async void OnSaved(object? sender, EventArgs e)
    {
        await RequestNotificationPermission();
        await Shell.Current.GoToAsync("..");
    }

    private async Task RequestNotificationPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status == PermissionStatus.Granted)
            return;

        var accepted = await DisplayAlertAsync(
            "Promemoria",
            "Centesimo può inviarti una notifica per ricordarti le prossime scadenze dei pagamenti regolari.",
            "Consenti",
            "Non ora");
        if (!accepted)
            return;

        if (await Permissions.RequestAsync<Permissions.PostNotifications>() == PermissionStatus.Granted)
            await _automation.ProcessDue();
    }
}
