using DatadogMauiApp.Services;

namespace DatadogMauiApp.Pages;

public partial class WebPortalPage : ContentPage
{
    private readonly ApiService _apiService;

    public WebPortalPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        LoadWebViewUrl();
    }

    private async void LoadWebViewUrl()
    {
        try
        {
            // Fetch configuration from API
            var config = await _apiService.GetConfigAsync();
            if (config != null && !string.IsNullOrEmpty(config.WebViewUrl))
            {
                WebViewControl.Source = config.WebViewUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to load config: {ex.Message}");
            // Fall back to default URL (already set in XAML)
        }
    }

    private void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (e.Result == WebNavigationResult.Success)
        {
            Console.WriteLine($"[Telemetry] WebView Navigated: {e.Url}");
        }
        else
        {
            Console.WriteLine($"[Telemetry] WebView Navigation Failed: {e.Url}");
        }
    }
}
