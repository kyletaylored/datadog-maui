using DatadogMauiApp.Config;
using System.Reflection;

namespace DatadogMauiApp.Pages;

public partial class DiagnosticsPage : ContentPage
{
    public DiagnosticsPage()
    {
        InitializeComponent();
        LoadDiagnostics();
    }

    private void LoadDiagnostics()
    {
        try
        {
            // Platform Information
            PlatformLabel.Text = GetPlatformName();
            AppVersionLabel.Text = GetAppVersion();
            FrameworkLabel.Text = GetFrameworkVersion();

            // Core Configuration
            ServiceNameLabel.Text = DatadogConfig.ServiceName;
            EnvironmentLabel.Text = DatadogConfig.Environment;
            SiteLabel.Text = DatadogConfig.Site.ToUpperInvariant();
            VerboseLoggingLabel.Text = DatadogConfig.VerboseLogging ? "Enabled" : "Disabled";

            // RUM Configuration
            RumApplicationIdLabel.Text = MaskSensitiveValue(DatadogConfig.RumApplicationId);
            ClientTokenLabel.Text = MaskSensitiveValue(DatadogConfig.ClientToken);
            SessionSampleRateLabel.Text = $"{DatadogConfig.SessionSampleRate}%";
            SessionReplayRateLabel.Text = $"{DatadogConfig.SessionReplaySampleRate}%";

            // Credential Source
            CredentialSourceLabel.Text = GetCredentialSource();

            // Last updated timestamp
            LastUpdatedLabel.Text = $"Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _ = DisplayAlertAsync("Error", $"Failed to load diagnostics: {ex.Message}", "OK");
        }
    }

    private string GetPlatformName()
    {
#if ANDROID
        return "Android";
#elif IOS
        return "iOS";
#elif MACCATALYST
        return "Mac Catalyst";
#elif WINDOWS
        return "Windows";
#else
        return "Unknown";
#endif
    }

    private string GetAppVersion()
    {
        try
        {
            var version = AppInfo.Current.VersionString;
            var build = AppInfo.Current.BuildString;
            return $"{version} (Build {build})";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string GetFrameworkVersion()
    {
        try
        {
            var frameworkName = Assembly.GetEntryAssembly()?.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>()?.FrameworkName;
            return frameworkName ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string MaskSensitiveValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "[Not Set]";

        if (value.StartsWith("PLACEHOLDER_", StringComparison.OrdinalIgnoreCase))
            return $"⚠️ {value} (using placeholder)";

        // Show first 8 and last 4 characters, mask the middle
        if (value.Length <= 12)
            return $"{value[..4]}...{value[^2..]}";

        return $"{value[..8]}...{value[^4..]}";
    }

    private string GetCredentialSource()
    {
        var clientToken = DatadogConfig.ClientToken;
        var appId = DatadogConfig.RumApplicationId;

        if (clientToken.StartsWith("PLACEHOLDER_", StringComparison.OrdinalIgnoreCase) ||
            appId.StartsWith("PLACEHOLDER_", StringComparison.OrdinalIgnoreCase))
        {
            return "⚠️ Using default placeholders\n\n" +
                   "Credentials not found in:\n" +
                   "• Embedded config file (generated at build time)\n" +
                   "• Environment variables\n\n" +
                   "Set DD_RUM_*_CLIENT_TOKEN and DD_RUM_*_APPLICATION_ID environment variables or configure MSBuild properties.";
        }

        // Try to determine actual source
        var platformKey = GetPlatformName().ToLowerInvariant();
        var envToken = System.Environment.GetEnvironmentVariable($"DD_RUM_{platformKey.ToUpperInvariant()}_CLIENT_TOKEN");

        if (!string.IsNullOrEmpty(envToken))
        {
            return "✅ Loaded from environment variables\n\n" +
                   $"DD_RUM_{platformKey.ToUpperInvariant()}_CLIENT_TOKEN and DD_RUM_{platformKey.ToUpperInvariant()}_APPLICATION_ID are set.";
        }

        return "✅ Loaded from embedded config file\n\n" +
               "Configuration file was generated at build time from MSBuild properties or environment variables.";
    }

    private void OnRefreshClicked(object sender, EventArgs e)
    {
        LoadDiagnostics();
    }
}
