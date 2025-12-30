using Android.App;
using Android.Runtime;
using Datadog.Android.Core.Configuration;
using Datadog.Android.Log;
using Datadog.Android.Ndk;
using Datadog.Android.Privacy;
using Datadog.Android.Rum;
using Datadog.Android.SessionReplay;
using Datadog.Android.SessionReplay.Material;
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
        // TODO: Update Datadog initialization for v2.21.0-pre.1 API
        // The current initialization code was written for an older API version
        // Refer to: https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings for examples
        InitializeDatadog();
    }

    private void InitializeDatadog()
    {
        // TODO: This initialization code was written for an older Datadog API version
        // and is incompatible with v2.21.0-pre.1. The API has changed significantly.
        //
        // To update this code:
        // 1. Visit: https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings
        // 2. Check the samples directory for Android initialization examples
        // 3. Update the code below to match the v2.21.0-pre.1 API
        //
        // Key API changes:
        // - SessionReplayConfiguration.Builder constructor signature changed
        // - SessionReplayPrivacy constructor changed
        // - SessionReplay.Enable method signature changed
        // - ExtensionSupport class location/namespace changed

        Console.WriteLine("[Datadog] Initialization disabled - needs API update for v2.21.0-pre.1");

        /*
        try
        {
            // Create Datadog configuration
            var config = new DDConfiguration.Builder(
                DatadogConfig.ClientToken,
                DatadogConfig.Environment,
                string.Empty,
                DatadogConfig.ServiceName
            ).Build();

            // Initialize Datadog SDK
            Datadog.Android.Datadog.Initialize(this, config, TrackingConsent.Granted);

            // Set verbosity level for debugging
            if (DatadogConfig.VerboseLogging)
            {
                Datadog.Android.Datadog.Verbosity = (int)Android.Util.LogPriority.Verbose;
            }

            // Enable Logs
            var logsConfig = new LogsConfiguration.Builder()
                .SetEventMapper(null)
                .Build();
            Logs.Enable(logsConfig);

            // Enable NDK crash reports
            NdkCrashReports.Enable();

            // Enable RUM (Real User Monitoring)
            var rumConfiguration = new RumConfiguration.Builder(DatadogConfig.RumApplicationId)
                .SetSessionSampleRate(DatadogConfig.SessionSampleRate)
                .TrackLongTasks()
                .TrackFrustrations(true)
                .TrackBackgroundEvents(true)
                .TrackNonFatalAnrs(true)
                .Build();

            Datadog.Android.Rum.Rum.Enable(rumConfiguration);

            // Enable Session Replay
            var replayConfig = new SessionReplayConfiguration.Builder(
                new[] { DatadogConfig.SessionReplaySampleRate })
                .SetPrivacy(new SessionReplayPrivacy(
                    TextAndInputPrivacy.MaskAll,
                    ImagePrivacy.MaskAll,
                    TouchPrivacy.Hide))
                .Build();

            SessionReplay.Enable(replayConfig, this);

            // Add Material extension for better UI capture
            ExtensionSupport.AddExtensionSupport(new MaterialExtensionSupport());

            // Initialize Global RUM Monitor
            _ = Datadog.Android.Rum.GlobalRumMonitor.Instance;

            Console.WriteLine("[Datadog] Successfully initialized for Android");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Datadog] Failed to initialize: {ex.Message}");
            Console.WriteLine($"[Datadog] Stack trace: {ex.StackTrace}");
        }
        */
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
