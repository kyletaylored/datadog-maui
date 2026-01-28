using Foundation;
using UIKit;
using Datadog.iOS.Core;
using Datadog.iOS.RUM;
using Datadog.iOS.Logs;
using Datadog.iOS.Trace;
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

            // Initialize the Datadog SDK with new API
            var config = new DDConfiguration(
                clientToken: DatadogConfig.ClientToken,
                env: DatadogConfig.Environment
            );

            config.Service = DatadogConfig.ServiceName;
            // Note: Site configuration depends on your Datadog site
            // config.Site = DDDatadogSite.Us1; // Uncomment and set appropriately

            DDDatadog.InitializeWithConfiguration(config, DDTrackingConsent.Granted);

            Console.WriteLine("[Datadog] Core SDK initialized");

            // Set verbosity level for debugging
            if (DatadogConfig.VerboseLogging)
            {
                DDDatadog.VerbosityLevel = DDCoreLoggerLevel.Debug;
            }

            // Enable Logs
            var logsConfiguration = new DDLogsConfiguration();
            DDLogs.EnableWith(logsConfiguration);

            Console.WriteLine("[Datadog] Logs enabled");

            // Enable RUM (Real User Monitoring)
            var rumConfig = new DDRUMConfiguration(applicationID: DatadogConfig.RumApplicationId);
            rumConfig.SessionSampleRate = DatadogConfig.SessionSampleRate;  // Accepts float directly
            rumConfig.TrackFrustrations = true;
            rumConfig.TrackBackgroundEvents = true;

            // Note: iOS URLSession tracking configuration is different from Android
            // For distributed tracing on iOS, you may need to configure URLSessionInstrumentation
            // separately using the native SDK if needed for advanced tracing scenarios.
            //
            // Basic RUM tracking (page views, user interactions, errors) works fine.

            DDRUM.EnableWith(rumConfig);

            Console.WriteLine("[Datadog] RUM enabled");

            // Enable APM Tracing
            try
            {
                var traceConfig = new DDTraceConfiguration();
                traceConfig.SampleRate = 100.0f;  // Sample 100% of traces
                DDTrace.EnableWith(traceConfig);
                Console.WriteLine("[Datadog] APM Tracing enabled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Datadog] APM Tracing failed: {ex.Message}");
            }

            Console.WriteLine("[Datadog] Successfully initialized for iOS");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Datadog] Failed to initialize: {ex.Message}");
            Console.WriteLine($"[Datadog] Stack trace: {ex.StackTrace}");
        }
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
