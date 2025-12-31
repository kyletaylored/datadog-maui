namespace DatadogMauiApp.Config;

public static class DatadogConfig
{
    // Platform-specific credentials
    // Android credentials
    private const string AndroidClientToken = "REDACTED_CLIENT_TOKEN_1";
    private const string AndroidRumApplicationId = "REDACTED_APP_ID_3";

    // iOS credentials
    private const string IosClientToken = "REDACTED_CLIENT_TOKEN_2";
    private const string IosRumApplicationId = "REDACTED_APP_ID_2";

    // Get platform-specific client token
    public static string ClientToken
    {
        get
        {
#if ANDROID
            return AndroidClientToken;
#elif IOS
            return IosClientToken;
#else
            return string.Empty;
#endif
        }
    }

    // Get platform-specific RUM Application ID
    public static string RumApplicationId
    {
        get
        {
#if ANDROID
            return AndroidRumApplicationId;
#elif IOS
            return IosRumApplicationId;
#else
            return string.Empty;
#endif
        }
    }

    // Environment (can be overridden via environment variable)
    public static string Environment
    {
        get
        {
            var env = System.Environment.GetEnvironmentVariable("DD_ENV");
            return !string.IsNullOrEmpty(env) ? env : "local";
        }
    }

    // Service name
    public const string ServiceName = "datadog-maui-app";

    // Datadog site (US1, EU1, US3, US5, US1_FED, AP1)
    public const string Site = "us1";

    // Session sample rate (0-100)
    // 100 = track all sessions
    public const float SessionSampleRate = 100f;

    // Session Replay sample rate (0-100)
    // 100 = replay all sessions
    public const float SessionReplaySampleRate = 100f;

    // Enable debug logging
    public const bool VerboseLogging = true;
}
