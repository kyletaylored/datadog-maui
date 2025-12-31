using System.Reflection;

namespace DatadogMauiApp.Config;

public static class DatadogConfig
{
    // Platform-specific credentials (fallback defaults)
    private const string DefaultAndroidClientToken = "PLACEHOLDER_ANDROID_CLIENT_TOKEN";
    private const string DefaultAndroidRumApplicationId = "PLACEHOLDER_ANDROID_APPLICATION_ID";
    private const string DefaultIosClientToken = "PLACEHOLDER_IOS_CLIENT_TOKEN";
    private const string DefaultIosRumApplicationId = "PLACEHOLDER_IOS_APPLICATION_ID";

    // Cached credentials loaded from embedded config file
    private static Dictionary<string, string>? _cachedCredentials;

    /// <summary>
    /// Load credentials from embedded config file (generated at build time)
    /// Priority: Embedded Config File > Environment Variable > Default Placeholder
    /// </summary>
    private static Dictionary<string, string> LoadCredentials()
    {
        if (_cachedCredentials != null)
            return _cachedCredentials;

        _cachedCredentials = new Dictionary<string, string>();

#if ANDROID
        var resourceName = "DatadogMauiApp.Platforms.Android.datadog-rum.config";
#elif IOS
        var resourceName = "DatadogMauiApp.Platforms.iOS.datadog-rum.config";
#else
        return _cachedCredentials;
#endif

        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var content = reader.ReadToEnd();

                // Parse the config file (format: KEY=VALUE\nKEY=VALUE)
                foreach (var line in content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = line.Trim().Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        _cachedCredentials[key] = value;
                        Console.WriteLine($"[DatadogConfig]   {key} = {value.Substring(0, Math.Min(10, value.Length))}...");
                    }
                }

                Console.WriteLine($"[DatadogConfig] ✅ Loaded {_cachedCredentials.Count} credentials from embedded config file");
            }
            else
            {
                Console.WriteLine($"[DatadogConfig] ⚠️  Config file not found in embedded resources");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DatadogConfig] ⚠️  Failed to load credentials: {ex.Message}");
        }

        return _cachedCredentials;
    }

    // Get platform-specific client token
    // Priority: Embedded Config > Environment Variable > Default Placeholder
    public static string ClientToken
    {
        get
        {
#if ANDROID
            var creds = LoadCredentials();
            if (creds.TryGetValue("DD_RUM_ANDROID_CLIENT_TOKEN", out var configToken) && !string.IsNullOrEmpty(configToken))
                return configToken;

            var envToken = System.Environment.GetEnvironmentVariable("DD_RUM_ANDROID_CLIENT_TOKEN");
            return !string.IsNullOrEmpty(envToken) ? envToken : DefaultAndroidClientToken;
#elif IOS
            var creds = LoadCredentials();
            if (creds.TryGetValue("DD_RUM_IOS_CLIENT_TOKEN", out var configToken) && !string.IsNullOrEmpty(configToken))
                return configToken;

            var envToken = System.Environment.GetEnvironmentVariable("DD_RUM_IOS_CLIENT_TOKEN");
            return !string.IsNullOrEmpty(envToken) ? envToken : DefaultIosClientToken;
#else
            return string.Empty;
#endif
        }
    }

    // Get platform-specific RUM Application ID
    // Priority: Embedded Config > Environment Variable > Default Placeholder
    public static string RumApplicationId
    {
        get
        {
#if ANDROID
            var creds = LoadCredentials();
            if (creds.TryGetValue("DD_RUM_ANDROID_APPLICATION_ID", out var configAppId) && !string.IsNullOrEmpty(configAppId))
                return configAppId;

            var envAppId = System.Environment.GetEnvironmentVariable("DD_RUM_ANDROID_APPLICATION_ID");
            return !string.IsNullOrEmpty(envAppId) ? envAppId : DefaultAndroidRumApplicationId;
#elif IOS
            var creds = LoadCredentials();
            if (creds.TryGetValue("DD_RUM_IOS_APPLICATION_ID", out var configAppId) && !string.IsNullOrEmpty(configAppId))
                return configAppId;

            var envAppId = System.Environment.GetEnvironmentVariable("DD_RUM_IOS_APPLICATION_ID");
            return !string.IsNullOrEmpty(envAppId) ? envAppId : DefaultIosRumApplicationId;
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
