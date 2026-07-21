using Centesimo.App.ViewModels;
using Centesimo.Application;

namespace Centesimo.App.Pages;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _viewModel;
    private readonly SpeechExpenseDraftService _speechDraftService;
    private readonly IItalianSpeechModelProvisioner _modelProvisioner;
    private Task<Result>? _speechStart;
    private int _speechSession;

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

    private void OnSpeechPressed(object? sender, EventArgs e)
    {
        var session = ++_speechSession;
        _speechStart = StartSpeech(session);
    }

    private async Task<Result> StartSpeech(int session)
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
                    return Result.Failure(provision.Error);
                }
            }

            var permission = await Permissions.RequestAsync<Permissions.Microphone>();
            if (permission != PermissionStatus.Granted)
                return Result.Failure(new Centesimo.Application.Error("Speech.MicrophoneDenied", "Consenti l'accesso al microfono per usare i comandi vocali."));

            var result = await _speechDraftService.Start();
            if (session != _speechSession || result.IsFailure)
            {
                if (result.IsSuccess)
                    await _speechDraftService.Cancel();
                return result;
            }

            _viewModel.SpeechErrorMessage = "";
            _viewModel.IsSpeechListening = true;
            _viewModel.IsSpeechProcessing = false;
            _viewModel.SpeechTranscription = "Tieni premuto il microfono e rilascia per elaborare.";
            return result;
        }
        catch
        {
            return Result.Failure(new Centesimo.Application.Error("Speech.StartFailed", "Non riesco ad avviare il riconoscimento vocale."));
        }
    }

    private async void OnSpeechReleased(object? sender, EventArgs e)
    {
        try
        {
            var start = _speechStart;
            if (start is null)
                return;

            var startResult = await start;
            _viewModel.IsSpeechListening = false;
            _viewModel.IsSpeechProcessing = true;
            _viewModel.IsSpeechSheetVisible = true;
            if (startResult.IsFailure)
            {
                _viewModel.IsSpeechProcessing = false;
                _viewModel.SpeechErrorMessage = $"Non ho capito bene. {startResult.Error.Message}";
                return;
            }

            var result = await _speechDraftService.StopAndPrepare();
            _viewModel.IsSpeechProcessing = false;
            _viewModel.SpeechTranscription = _speechDraftService.LastTranscription;
            if (result.IsFailure)
            {
                _viewModel.SpeechErrorMessage = $"Non ho capito bene. {result.Error.Message}";
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
            _speechStart = null;
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
