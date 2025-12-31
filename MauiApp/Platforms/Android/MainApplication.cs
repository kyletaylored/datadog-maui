using Android.App;
using Android.Runtime;
using Datadog.Android.Core.Configuration;
using Datadog.Android.Log;
using Datadog.Android.Ndk;
using Datadog.Android.Privacy;
using Datadog.Android.Rum;
using DatadogMauiApp.Config;

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
            // Parameters: clientToken, env, variant, serviceName
            var config = new DDConfiguration.Builder(
                DatadogConfig.ClientToken,
                DatadogConfig.Environment,
                string.Empty,  // variant - use empty string if no build variants
                DatadogConfig.ServiceName
            )
            //.UseSite(DatadogSite.Us1)  // Uncomment if needed
            .Build();

            // Initialize Datadog SDK
            Datadog.Android.Datadog.Initialize(this, config, TrackingConsent.Granted);

            Console.WriteLine("[Datadog] Core SDK initialized");

            // Set verbosity level for debugging
            if (DatadogConfig.VerboseLogging)
            {
                Datadog.Android.Datadog.Verbosity = (int)Android.Util.LogPriority.Verbose;
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

            Datadog.Android.Rum.Rum.Enable(rumConfiguration);

            Console.WriteLine("[Datadog] RUM enabled");

            // Initialize Global RUM Monitor
            _ = Datadog.Android.Rum.GlobalRumMonitor.Instance;
            _ = Datadog.Android.Rum.GlobalRumMonitor.Get();

            Console.WriteLine("[Datadog] Successfully initialized for Android");

            // TODO: Add Session Replay, APM Tracing, and WebView tracking
            // These features require additional using statements and configuration
            // See comments below for implementation details
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Datadog] Failed to initialize: {ex.Message}");
            Console.WriteLine($"[Datadog] Stack trace: {ex.StackTrace}");
        }
    }

    /* TODO: Advanced Datadog Features
     *
     * To enable these features, add the following using statements:
     * using Datadog.Android.SessionReplay;
     * using Datadog.Android.Trace;
     * using Datadog.Android.WebView;
     *
     * SESSION REPLAY:
     * ---------------
     * var sessionReplayConfig = new SessionReplayConfiguration.Builder(
     *     new[] { DatadogConfig.SessionReplaySampleRate }
     * )
     *     .SetPrivacy(new SessionReplayPrivacy(
     *         TextAndInputPrivacy.MaskAll,
     *         ImagePrivacy.MaskAll,
     *         TouchPrivacy.Hide
     *     ))
     *     .Build();
     * SessionReplay.Enable(sessionReplayConfig);
     * SessionReplay.Instance?.StartRecording();
     *
     * APM TRACING:
     * ------------
     * var traceConfig = new TraceConfiguration.Builder().Build();
     * Datadog.Android.Trace.Trace.Enable(traceConfig);
     *
     * var tracer = DatadogTracing.NewTracerBuilder(Datadog.Android.Datadog.Instance)
     *     .WithServiceName(DatadogConfig.ServiceName)
     *     .SetBundleWithRumEnabled(true)
     *     .Build();
     * GlobalDatadogTracer.RegisterIfAbsent(tracer);
     *
     * WEBVIEW TRACKING:
     * -----------------
     * To enable tracking for a specific WebView:
     *
     * webView.Settings.JavaScriptEnabled = true;
     * WebViewTracking.Enable(webView, new[] { "example.com", "api.example.com" });
     *
     * Call this method in your WebView page after creating the WebView instance.
     */

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
