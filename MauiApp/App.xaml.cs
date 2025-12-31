namespace DatadogMauiApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Log app start
        Console.WriteLine("[Telemetry] App Start");
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
