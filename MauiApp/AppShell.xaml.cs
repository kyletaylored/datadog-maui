namespace DatadogMauiApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Track tab changes
        this.Navigated += (s, e) =>
        {
            Console.WriteLine($"[Telemetry] Tab Changed: {e.Current.Location}");
        };
    }
}
