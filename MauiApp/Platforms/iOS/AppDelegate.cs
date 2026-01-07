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

            // Enable RUM (Real User Monitoring)
            var rumConfig = new DDRUMConfiguration(DatadogConfig.RumApplicationId);
            rumConfig.SessionSampleRate = DatadogConfig.SessionSampleRate;
            rumConfig.TrackFrustrations = true;
            rumConfig.TrackBackgroundEvents = true;
            DDRUM.Enable(rumConfig);

            Console.WriteLine("[Datadog] RUM enabled");

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
