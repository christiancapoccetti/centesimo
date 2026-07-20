namespace Centesimo.App;

public partial class App : Microsoft.Maui.Controls.Application
{
    private readonly Func<AppShell> _appShellFactory;

    public App(Func<AppShell> appShellFactory)
    {
        InitializeComponent();
        _appShellFactory = appShellFactory;
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new(_appShellFactory());
}
