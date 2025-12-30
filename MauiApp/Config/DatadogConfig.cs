namespace DatadogMauiApp.Config;

public static class DatadogConfig
{
    // TODO: Replace these with your actual Datadog credentials
    // Get these from: https://app.datadoghq.com/organization-settings/client-tokens
    public const string ClientToken = "YOUR_CLIENT_TOKEN_HERE";

    // TODO: Replace with your RUM Application ID
    // Get this from: https://app.datadoghq.com/rum/list
    public const string RumApplicationId = "YOUR_RUM_APPLICATION_ID_HERE";

    // Environment (e.g., "dev", "staging", "prod")
    public const string Environment = "dev";

    // Service name
    public const string ServiceName = "datadog-maui-app";

    // Datadog site (US1, EU1, US3, US5, US1_FED, AP1)
    // For US: DatadogSite.Us1
    // For EU: DatadogSite.Eu1
    public const string Site = "us1"; // Change to your site

    // Session sample rate (0-100)
    // 100 = track all sessions
    public const float SessionSampleRate = 100f;

    // Session Replay sample rate (0-100)
    // 100 = replay all sessions
    public const float SessionReplaySampleRate = 100f;

    // Enable debug logging
    public const bool VerboseLogging = true;
}
