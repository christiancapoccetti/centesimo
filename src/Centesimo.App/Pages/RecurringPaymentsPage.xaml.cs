using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class RecurringPaymentsPage : ContentPage
{
    private readonly RecurringPaymentsViewModel _viewModel;
    public RecurringPaymentsPage(RecurringPaymentsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }
    protected override async void OnAppearing() { base.OnAppearing(); await _viewModel.Load(); }
    private async void OnAddClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(RecurringPaymentEditorPage));
    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: RecurringPaymentsViewModel.RecurringPaymentItemViewModel payment }) return;
        await Shell.Current.GoToAsync($"{nameof(RecurringPaymentEditorPage)}?paymentId={payment.RecurringPaymentId}");
    }
    private async void OnSuspendResumeClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: RecurringPaymentsViewModel.RecurringPaymentItemViewModel payment }) return;
        await _viewModel.SuspendOrResume(payment);
    }
    private async void OnEndClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: RecurringPaymentsViewModel.RecurringPaymentItemViewModel payment }) return;
        if (await DisplayAlertAsync("Termina pagamento", "Non verranno create nuove spese dopo la prossima scadenza.", "Termina", "Annulla"))
            await _viewModel.End(payment);
    }
}
