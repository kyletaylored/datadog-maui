namespace DatadogMauiApp.Models;

public class ConfigResponse
{
    public string WebViewUrl { get; set; } = string.Empty;
    public Dictionary<string, bool> FeatureFlags { get; set; } = new();
}
