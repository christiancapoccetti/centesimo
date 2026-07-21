namespace Centesimo.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly Func<AppShell> _appShellFactory;
    private readonly RecurringPaymentAutomation _automation;
    public App(Func<AppShell> appShellFactory, RecurringPaymentAutomation automation)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        _appShellFactory = appShellFactory;
        _automation = automation;
    }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(_appShellFactory());
        window.Activated += OnWindowActivated;
        return window;
    }

    private void OnWindowActivated(object? sender, EventArgs eventArgs) => _ = _automation.ProcessDue();
}
