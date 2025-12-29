namespace DatadogMauiApi.Models;

public record ConfigResponse(
    string WebViewUrl,
    Dictionary<string, bool> FeatureFlags
);
