namespace DatadogMauiApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Log app start
        Console.WriteLine("[Telemetry] App Start");

        MainPage = new AppShell();
    }
}
