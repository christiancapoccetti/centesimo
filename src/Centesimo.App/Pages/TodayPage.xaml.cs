using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _viewModel;
    private readonly SpeechExpenseDraftService _speechDraftService;
    private readonly IItalianSpeechModelProvisioner _modelProvisioner;

    public TodayPage(TodayViewModel viewModel, SpeechExpenseDraftService speechDraftService, IItalianSpeechModelProvisioner modelProvisioner)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _speechDraftService = speechDraftService;
        _modelProvisioner = modelProvisioner;
        _speechDraftService.TranscriptionUpdated += OnTranscriptionUpdated;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        if (!_speechDraftService.IsListening)
            return;

        await _speechDraftService.Cancel();
        _viewModel.IsSpeechListening = false;
        _viewModel.IsSpeechProcessing = false;
        _viewModel.IsSpeechSheetVisible = false;
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

    private async void OnSpeechPressed(object? sender, EventArgs e)
    {
        try
        {
            if (!_modelProvisioner.IsAvailable)
            {
                _viewModel.IsSpeechProcessing = true;
                _viewModel.SpeechTranscription = "Preparazione del riconoscimento vocale…";
                var progress = new Progress<double>(value => _viewModel.SpeechTranscription = $"Preparazione del modello: {value:P0}");
                var provision = await _modelProvisioner.Prepare(progress);
                _viewModel.IsSpeechProcessing = false;
                if (provision.IsFailure)
                {
                    _viewModel.SpeechErrorMessage = provision.Error.Message;
                    _viewModel.IsSpeechSheetVisible = true;
                    return;
                }
            }

            var permission = await Permissions.RequestAsync<Permissions.Microphone>();
            if (permission != PermissionStatus.Granted)
            {
                _viewModel.SpeechErrorMessage = "Consenti l'accesso al microfono per usare i comandi vocali.";
                _viewModel.IsSpeechSheetVisible = true;
                return;
            }

            var result = await _speechDraftService.Start();
            _viewModel.SpeechErrorMessage = result.IsFailure ? result.Error.Message : "";
            _viewModel.IsSpeechSheetVisible = result.IsFailure;
            _viewModel.IsSpeechListening = result.IsSuccess;
            _viewModel.IsSpeechProcessing = false;
            _viewModel.SpeechTranscription = "Tieni premuto il microfono e rilascia per elaborare.";
        }
        catch
        {
            _viewModel.SpeechErrorMessage = "Non riesco ad avviare il riconoscimento vocale.";
            _viewModel.IsSpeechSheetVisible = true;
        }
    }

    private async void OnSpeechReleased(object? sender, EventArgs e)
    {
        try
        {
            _viewModel.IsSpeechListening = false;
            _viewModel.IsSpeechProcessing = true;
            _viewModel.IsSpeechSheetVisible = true;
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
        catch
        {
            _viewModel.IsSpeechProcessing = false;
            _viewModel.SpeechErrorMessage = "Non riesco a elaborare il comando vocale.";
        }
        finally
        {
            _viewModel.IsSpeechListening = false;
            if (_viewModel.HasSpeechError)
                _viewModel.IsSpeechSheetVisible = true;
        }
    }

    private void OnCloseSpeechClicked(object? sender, EventArgs e)
    {
        _viewModel.IsSpeechListening = false;
        _viewModel.IsSpeechProcessing = false;
        _viewModel.IsSpeechSheetVisible = false;
        _viewModel.SpeechErrorMessage = "";
    }

    private void OnRetrySpeechClicked(object? sender, EventArgs e)
    {
        OnCloseSpeechClicked(sender, e);
        _viewModel.SpeechTranscription = "Tieni premuto il microfono per riprovare.";
    }

    private void OnTranscriptionUpdated(object? sender, string transcription) =>
        MainThread.BeginInvokeOnMainThread(() => _viewModel.SpeechTranscription = transcription);
}
