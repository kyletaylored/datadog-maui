using Foundation;
using UIKit;
using Datadog.iOS.ObjC;
using DatadogMauiApp.Config;

namespace DatadogMauiApp;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        InitializeDatadog();
        return base.FinishedLaunching(application, launchOptions);
    }

    private void InitializeDatadog()
    {
        try
        {
            Console.WriteLine($"[Datadog] Initializing for iOS");
            Console.WriteLine($"[Datadog] - Environment: {DatadogConfig.Environment}");
            Console.WriteLine($"[Datadog] - Client Token: {DatadogConfig.ClientToken.Substring(0, 10)}...{DatadogConfig.ClientToken.Substring(DatadogConfig.ClientToken.Length - 4)}");
            Console.WriteLine($"[Datadog] - RUM Application ID: {DatadogConfig.RumApplicationId}");

            // Initialize the Datadog SDK
            var config = new DDConfiguration(
                DatadogConfig.ClientToken,
                DatadogConfig.Environment
            );

            config.Service = DatadogConfig.ServiceName;
            // Note: Site configuration depends on your Datadog site
            // config.Site = DDDatadogSite.Us1; // Uncomment and set appropriately

            DDDatadog.Initialize(config, DDTrackingConsent.Granted);

            Console.WriteLine("[Datadog] Core SDK initialized");

            // Set verbosity level for debugging
            if (DatadogConfig.VerboseLogging)
            {
                DDDatadog.VerbosityLevel = DDSDKVerbosityLevel.Debug;
            }

            // Enable Logs
            DDLogs.Enable(new DDLogsConfiguration(null));

            Console.WriteLine("[Datadog] Logs enabled");

            // Enable RUM (Real User Monitoring) with URL session tracking for APM
            var rumConfig = new DDRUMConfiguration(DatadogConfig.RumApplicationId);
            rumConfig.SessionSampleRate = DatadogConfig.SessionSampleRate;
            rumConfig.TrackFrustrations = true;
            rumConfig.TrackBackgroundEvents = true;

            // Note: iOS URLSession tracking configuration is different from Android
            // The C# bindings (Bcr.Datadog.iOS v2.26.0) don't expose URLSessionInstrumentation APIs
            // For distributed tracing on iOS, the native SDK requires:
            // 1. URLSessionInstrumentation.enable() - not available in C# bindings yet
            // 2. First-party hosts configured in Trace.Configuration.URLSessionTracking
            //
            // Basic RUM tracking (page views, user interactions, errors) works fine.
            // Network tracing and trace correlation with backend may require native iOS code or binding updates.

            DDRUM.Enable(rumConfig);

            Console.WriteLine("[Datadog] RUM enabled");
            Console.WriteLine("[Datadog] Note: URLSession tracking requires native iOS APIs not yet available in C# bindings");

            // Enable APM Tracing
            try
            {
                DDTrace.Enable(new DDTraceConfiguration());
                _ = DDTracer.Shared;
                Console.WriteLine("[Datadog] APM Tracing enabled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Datadog] APM Tracing failed: {ex.Message}");
            }

            Console.WriteLine("[Datadog] Successfully initialized for iOS");
            Console.WriteLine("[Datadog] Note: Crash Reporting and Session Replay require additional packages (Bcr.Datadog.iOS.CR, Bcr.Datadog.iOS.SR)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Datadog] Failed to initialize: {ex.Message}");
            Console.WriteLine($"[Datadog] Stack trace: {ex.StackTrace}");
        }
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
