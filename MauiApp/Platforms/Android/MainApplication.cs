using Android.App;
using Android.Runtime;
using Com.Datadog.Android;
using Com.Datadog.Android.Core.Configuration;
using Com.Datadog.Android.Log;
using Com.Datadog.Android.Ndk;
using Com.Datadog.Android.Privacy;
using Com.Datadog.Android.Rum;
using Com.Datadog.Android.Sessionreplay;
using Com.Datadog.Android.Trace;
using DatadogConfig = DatadogMauiApp.Config.DatadogConfig;

namespace DatadogMauiApp;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();
        InitializeDatadog();
    }

    private void InitializeDatadog()
    {
        try
        {
            Console.WriteLine($"[Datadog] Initializing for Android");
            Console.WriteLine($"[Datadog] - Environment: {DatadogConfig.Environment}");
            Console.WriteLine($"[Datadog] - Client Token: {DatadogConfig.ClientToken.Substring(0, 10)}...{DatadogConfig.ClientToken.Substring(DatadogConfig.ClientToken.Length - 4)}");
            Console.WriteLine($"[Datadog] - RUM Application ID: {DatadogConfig.RumApplicationId}");

            // Create Datadog configuration
            // Parameters: clientToken, env, variant, serviceName (as positional args)
            var config = new Configuration.Builder(
                DatadogConfig.ClientToken,
                DatadogConfig.Environment,
                string.Empty,  // variant - use empty string if no build variants
                DatadogConfig.ServiceName
            )
            //.UseSite(DatadogSite.Us1)  // Uncomment if needed
            .Build();

            // Initialize Datadog SDK
            Com.Datadog.Android.Datadog.Initialize(this, config, TrackingConsent.Granted!);

            Console.WriteLine("[Datadog] Core SDK initialized");

            // Set verbosity level for debugging
            if (DatadogConfig.VerboseLogging)
            {
                Com.Datadog.Android.Datadog.Verbosity = (int)Android.Util.LogPriority.Verbose;
            }

            // Enable Logs
            var logsConfig = new LogsConfiguration.Builder().Build();
            Logs.Enable(logsConfig);

            Console.WriteLine("[Datadog] Logs enabled");

            // Enable NDK crash reports
            NdkCrashReports.Enable();

            Console.WriteLine("[Datadog] NDK crash reports enabled");

            // Enable RUM (Real User Monitoring)
            var rumConfiguration = new RumConfiguration.Builder(DatadogConfig.RumApplicationId)
                .TrackLongTasks()
                .TrackFrustrations(true)
                .TrackBackgroundEvents(true)
                .TrackNonFatalAnrs(true)
                .Build();

            Com.Datadog.Android.Rum.Rum.Enable(rumConfiguration);

            Console.WriteLine("[Datadog] RUM enabled");

            // Initialize Global RUM Monitor
            _ = Com.Datadog.Android.Rum.GlobalRumMonitor.Instance;
            _ = Com.Datadog.Android.Rum.GlobalRumMonitor.Get();

            // Enable Session Replay
            try
            {
                var sessionReplayConfig = new SessionReplayConfiguration.Builder(
                    DatadogConfig.SessionReplaySampleRate
                )
                .SetTextAndInputPrivacy(TextAndInputPrivacy.MaskSensitiveInputs!) // MaskAll | MaskSensitiveInputs | AllowAll
                .SetImagePrivacy(ImagePrivacy.MaskNone!) // MaskAll | MaskNonBundledOnly | MaskNone
                .SetTouchPrivacy(TouchPrivacy.Show!) // Hide | Show
                .Build();

                Com.Datadog.Android.Sessionreplay.SessionReplay.Enable(sessionReplayConfig, Com.Datadog.Android.Datadog.Instance);
                Console.WriteLine("[Datadog] Session Replay enabled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Datadog] Session Replay failed: {ex.Message}");
            }

            // Enable APM Tracing
            try
            {
                var traceConfig = new TraceConfiguration.Builder().Build();
                Com.Datadog.Android.Trace.Trace.Enable(traceConfig, Com.Datadog.Android.Datadog.Instance);
                Console.WriteLine("[Datadog] APM Tracing enabled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Datadog] APM Tracing failed: {ex.Message}");
            }

            Console.WriteLine("[Datadog] Successfully initialized for Android");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Datadog] Failed to initialize: {ex.Message}");
            Console.WriteLine($"[Datadog] Stack trace: {ex.StackTrace}");
        }
    }

    /* TODO: Additional Datadog Features (WebView Tracking)
     *
     * WEBVIEW TRACKING:
     * -----------------
     * Package installed: Bcr.Datadog.Android.Sdk.WebView
     * Should enable tracking for WebViews in WebPortalPage
     * See DATADOG_ADVANCED_FEATURES.md for guidance
     */

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
