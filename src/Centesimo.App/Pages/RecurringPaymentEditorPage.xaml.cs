using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class RecurringPaymentEditorPage : ContentPage, IQueryAttributable
{
    private readonly RecurringPaymentEditorViewModel _viewModel;
    private Guid? _paymentId;
    public RecurringPaymentEditorPage(RecurringPaymentEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
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
    private async void OnSaved(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
