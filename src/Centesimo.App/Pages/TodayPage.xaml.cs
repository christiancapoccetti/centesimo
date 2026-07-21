using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _viewModel;
    private readonly SpeechExpenseDraftService _speechDraftService;

    public TodayPage(TodayViewModel viewModel, SpeechExpenseDraftService speechDraftService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _speechDraftService = speechDraftService;
        _speechDraftService.TranscriptionUpdated += OnTranscriptionUpdated;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnPreviousMonthClicked(object? sender, EventArgs e) =>
        await _viewModel.PreviousMonth();

    private async void OnNextMonthClicked(object? sender, EventArgs e) =>
        await _viewModel.NextMonth();

    private async void OnExpenseClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: TodayViewModel.MonthlyExpenseItemViewModel expense })
            return;

        await Shell.Current.GoToAsync($"{nameof(ExpenseEditorPage)}?expenseId={expense.ExpenseId}");
    }

    private async void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: TodayViewModel.MonthlyCategoryItemViewModel category })
            return;

        await Shell.Current.GoToAsync($"{nameof(CategorySpendingPage)}?categoryId={category.CategoryId}&year={_viewModel.SelectedYear}&month={_viewModel.SelectedMonth}");
    }
    private void OnCardPressed(object? sender, EventArgs e) =>
        InteractionFeedback.Press(sender);

    private void OnCardReleased(object? sender, EventArgs e) =>
        InteractionFeedback.Release(sender);

    private async void OnAddExpenseClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(ExpenseEditorPage));

    private async void OnSpeechClicked(object? sender, EventArgs e)
    {
        var permission = await Permissions.RequestAsync<Permissions.Microphone>();
        if (permission != PermissionStatus.Granted)
        {
            _viewModel.SpeechErrorMessage = "Consenti l'accesso al microfono per usare i comandi vocali.";
            _viewModel.IsSpeechSheetVisible = true;
            return;
        }

        var result = await _speechDraftService.Start();
        _viewModel.SpeechErrorMessage = result.IsFailure ? result.Error.Message : "";
        _viewModel.IsSpeechSheetVisible = true;
        _viewModel.IsSpeechListening = result.IsSuccess;
        _viewModel.IsSpeechProcessing = false;
        _viewModel.SpeechTranscription = "Parla ora, poi premi Ferma registrazione.";
    }

    private async void OnStopSpeechClicked(object? sender, EventArgs e)
    {
        _viewModel.IsSpeechListening = false;
        _viewModel.IsSpeechProcessing = true;
        var result = await _speechDraftService.StopAndPrepare();
        _viewModel.IsSpeechProcessing = false;
        if (result.IsFailure)
        {
            _viewModel.SpeechErrorMessage = result.Error.Message;
            return;
        }

        _viewModel.IsSpeechSheetVisible = false;
        await Shell.Current.GoToAsync(nameof(ExpenseEditorPage));
    }

    private void OnTranscriptionUpdated(object? sender, string transcription) =>
        MainThread.BeginInvokeOnMainThread(() => _viewModel.SpeechTranscription = transcription);
}
