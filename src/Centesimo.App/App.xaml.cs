namespace Centesimo.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly Func<AppShell> _appShellFactory;
    private readonly RecurringPaymentAutomation _automation;
    private readonly IItalianSpeechModelProvisioner _modelProvisioner;
    private readonly SpeechExpenseDraftService _speechDraftService;
    private readonly SpeechPreparationStatus _speechPreparationStatus;
    private bool _hasPromptedForSpeech;

    public App(
        Func<AppShell> appShellFactory,
        RecurringPaymentAutomation automation,
        IItalianSpeechModelProvisioner modelProvisioner,
        SpeechExpenseDraftService speechDraftService,
        SpeechPreparationStatus speechPreparationStatus)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        _appShellFactory = appShellFactory;
        _automation = automation;
        _modelProvisioner = modelProvisioner;
        _speechDraftService = speechDraftService;
        _speechPreparationStatus = speechPreparationStatus;
    }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_appShellFactory());
        window.Activated += OnWindowActivated;
        return window;
    }

    private void OnWindowActivated(object? sender, EventArgs eventArgs)
    {
        _ = _automation.ProcessDue();
        _ = PrepareSpeechAtStartup();
    }

    private async Task PrepareSpeechAtStartup()
    {
        if (_hasPromptedForSpeech)
            return;

        if (await _modelProvisioner.IsAvailable())
        {
            _speechPreparationStatus.IsPreparing = true;
            _speechPreparationStatus.Message = "Caricamento del riconoscimento vocale…";
            await _speechDraftService.WarmUp();
            _speechPreparationStatus.IsPreparing = false;
            _speechPreparationStatus.IsReady = true;
            _speechPreparationStatus.Progress = 1;
            _speechPreparationStatus.Message = "Comandi vocali pronti.";
            return;
        }

        _hasPromptedForSpeech = true;
        var accepted = await Shell.Current.DisplayAlertAsync(
            "Comandi vocali",
            "Vuoi scaricare il modello per usare i comandi vocali? Occupa circa 181 MB ed elabora l'audio solo sul dispositivo.",
            "Sì, scarica",
            "No");
        if (!accepted)
            return;

        _speechPreparationStatus.IsPreparing = true;
        _speechPreparationStatus.Message = "Download del modello vocale…";
        var progress = new Progress<double>(value =>
        {
            _speechPreparationStatus.Progress = value;
            _speechPreparationStatus.Message = $"Download del modello vocale: {value:P0}";
        });
        var provision = await _modelProvisioner.Prepare(progress);
        if (provision.IsSuccess)
        {
            _speechPreparationStatus.Message = "Caricamento del riconoscimento vocale…";
            await _speechDraftService.WarmUp();
            _speechPreparationStatus.Progress = 1;
            _speechPreparationStatus.IsReady = true;
            _speechPreparationStatus.Message = "Comandi vocali pronti.";
        }

        _speechPreparationStatus.IsPreparing = false;
    }
}
