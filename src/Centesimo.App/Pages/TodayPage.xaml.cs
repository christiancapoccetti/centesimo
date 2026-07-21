using Centesimo.App.ViewModels;
using Centesimo.Application;

namespace Centesimo.App.Pages;

public partial class TodayPage : ContentPage
{
    private readonly TodayViewModel _viewModel;
    private readonly SpeechExpenseDraftService _speechDraftService;
    private readonly IItalianSpeechModelProvisioner _modelProvisioner;
    private Task<Result>? _speechStart;
    private Task? _speechPreparation;
    private bool _hasAskedToPrepareSpeech;
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
        if (await _modelProvisioner.IsAvailable())
            StartSpeechPreparation();
        else if (!_hasAskedToPrepareSpeech)
            await AskToPrepareSpeech();
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

    private async void OnOverviewTapped(object? sender, TappedEventArgs e) =>
        await _viewModel.ToggleOverview();

    private async void OnExpenseClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: TodayViewModel.MonthlyExpenseItemViewModel expense })
            return;

        await Shell.Current.GoToAsync($"{nameof(ExpenseEditorPage)}?expenseId={expense.ExpenseId}");
    }

    private async void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsYearlyOverview)
            return;

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
        if (!_viewModel.IsSpeechReady)
            return;

        var session = ++_speechSession;
        _speechStart = StartSpeech(session);
    }

    private async Task<Result> StartSpeech(int session)
    {
        try
        {
            if (!_viewModel.IsSpeechReady)
                return Result.Failure(new Centesimo.Application.Error("Speech.Preparing", "Il riconoscimento vocale è ancora in preparazione."));

            var permission = await RequestMicrophonePermission();
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

    private async Task<PermissionStatus> RequestMicrophonePermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status == PermissionStatus.Granted)
            return status;

        var accepted = await DisplayAlertAsync(
            "Microfono",
            "Centesimo usa il microfono solo mentre tieni premuto il pulsante dei comandi vocali. L'audio viene elaborato sul dispositivo e non viene inviato online.",
            "Consenti",
            "Non ora");
        return accepted
            ? await Permissions.RequestAsync<Permissions.Microphone>()
            : status;
    }

    private async void OnSpeechReleased(object? sender, EventArgs e)
    {
        try
        {
            var session = _speechSession;
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

    private async void OnRetrySpeechPreparationClicked(object? sender, EventArgs e) =>
        await AskToPrepareSpeech();

    private async void OnInactiveSpeechClicked(object? sender, EventArgs e) =>
        await AskToPrepareSpeech();

    private void StartSpeechPreparation()
    {
        if (_viewModel.IsSpeechReady || _speechPreparation is { IsCompleted: false })
            return;

        _speechPreparation = PrepareSpeech();
    }

    private async Task PrepareSpeech()
    {
        _viewModel.IsSpeechReady = false;
        _viewModel.IsSpeechPreparing = true;
        _viewModel.IsSpeechPreparationFailed = false;
        _viewModel.SpeechAvailabilityMessage = "Preparazione del riconoscimento vocale…";
        try
        {
            var progress = new Progress<double>(value =>
                _viewModel.SpeechAvailabilityMessage = $"Download del riconoscimento vocale: {value:P0}");
            var provision = await _modelProvisioner.Prepare(progress);
            if (provision.IsFailure)
            {
                _viewModel.SpeechAvailabilityMessage = $"Riconoscimento vocale non disponibile. {provision.Error.Message}";
                _viewModel.IsSpeechPreparationFailed = true;
                _viewModel.SpeechPreparationActionText = "Riprova";
                return;
            }

            _viewModel.SpeechAvailabilityMessage = "Caricamento del riconoscimento vocale…";
            var warmUp = await _speechDraftService.WarmUp();
            if (warmUp.IsFailure)
            {
                _viewModel.SpeechAvailabilityMessage = $"Riconoscimento vocale non disponibile. {warmUp.Error.Message}";
                _viewModel.IsSpeechPreparationFailed = true;
                _viewModel.SpeechPreparationActionText = "Riprova";
                return;
            }

            _viewModel.SpeechAvailabilityMessage = "";
            _viewModel.IsSpeechReady = true;
        }
        catch
        {
            _viewModel.SpeechAvailabilityMessage = "Riconoscimento vocale non disponibile. Tocca Riprova.";
            _viewModel.IsSpeechPreparationFailed = true;
            _viewModel.SpeechPreparationActionText = "Riprova";
        }
        finally
        {
            _viewModel.IsSpeechPreparing = false;
        }
    }

    private async Task AskToPrepareSpeech()
    {
        if (_viewModel.IsSpeechReady || _viewModel.IsSpeechPreparing)
            return;

        _hasAskedToPrepareSpeech = true;

        var accepted = await DisplayAlertAsync(
            "Comandi vocali",
            "Vuoi scaricare il modello per usare i comandi vocali? Occupa circa 181 MB ed elabora l'audio solo sul dispositivo.",
            "Sì, scarica",
            "No");
        if (accepted)
        {
            StartSpeechPreparation();
            return;
        }

        _viewModel.IsSpeechPreparationFailed = true;
        _viewModel.SpeechPreparationActionText = "Attiva";
        _viewModel.SpeechAvailabilityMessage = "Comandi vocali non attivati.";
    }

    private void OnTranscriptionUpdated(object? sender, string transcription) =>
        MainThread.BeginInvokeOnMainThread(() => _viewModel.SpeechTranscription = transcription);
}
