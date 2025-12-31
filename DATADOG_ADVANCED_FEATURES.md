# Datadog Advanced Features

This guide shows how to enable additional Datadog features: Session Replay, APM Tracing, and WebView tracking.

---

## Current Status

✅ **Enabled Features:**
- Core Datadog SDK
- RUM (Real User Monitoring)
- Logs
- NDK Crash Reports

⚠️ **Optional Features (Not Yet Enabled):**
- Session Replay
- APM Tracing
- WebView Tracking

The packages are already installed in [DatadogMauiApp.csproj](MauiApp/DatadogMauiApp.csproj):
- `Bcr.Datadog.Android.Sdk.SessionReplay`
- `Bcr.Datadog.Android.Sdk.Trace`
- `Bcr.Datadog.Android.Sdk.WebView`

---

## How to Enable Session Replay

Session Replay records user sessions as video playback with privacy masking.

### Step 1: Add Using Statements

In [MainApplication.cs](MauiApp/Platforms/Android/MainApplication.cs), add:

```csharp
using Datadog.Android.SessionReplay;
```

### Step 2: Enable Session Replay

Add this code after RUM is enabled (around line 83):

```csharp
// Enable Session Replay with privacy settings
var sessionReplayConfig = new SessionReplayConfiguration.Builder(
    new[] { DatadogConfig.SessionReplaySampleRate }
)
    .SetPrivacy(new SessionReplayPrivacy(
        TextAndInputPrivacy.MaskAll,      // Mask all text
        ImagePrivacy.MaskAll,              // Mask all images
        TouchPrivacy.Hide                  // Hide touch interactions
    ))
    .Build();

SessionReplay.Enable(sessionReplayConfig);

Console.WriteLine("[Datadog] Session Replay enabled with privacy masking");

// Start recording
SessionReplay.Instance?.StartRecording();

Console.WriteLine("[Datadog] Session Replay recording started");
```

### Privacy Settings

You can adjust privacy levels:

**Text and Input:**
- `TextAndInputPrivacy.MaskAll` - Masks all text (most private)
- `TextAndInputPrivacy.MaskSensitiveInputs` - Masks passwords, emails, etc.
- `TextAndInputPrivacy.AllowAll` - Shows all text

**Images:**
- `ImagePrivacy.MaskAll` - Masks all images (most private)
- `ImagePrivacy.MaskNonBundledOnly` - Masks downloaded images only
- `ImagePrivacy.MaskNone` - Shows all images

**Touch:**
- `TouchPrivacy.Hide` - Hides touch interactions (most private)
- `TouchPrivacy.Show` - Shows where users tap

### Verify in Datadog

1. Run the app
2. Navigate through screens
3. Wait 1-2 minutes
4. Go to [Datadog Session Replay](https://app.datadoghq.com/rum/replay/sessions)
5. Find your session and watch the replay

---

## How to Enable APM Tracing

APM Tracing provides distributed tracing for requests and operations.

### Step 1: Add Using Statements

In [MainApplication.cs](MauiApp/Platforms/Android/MainApplication.cs), add:

```csharp
using Datadog.Android.Trace;
```

### Step 2: Enable Tracing

Add this code after RUM is enabled:

```csharp
// Enable APM Tracing
var traceConfig = new TraceConfiguration.Builder().Build();
Datadog.Android.Trace.Trace.Enable(traceConfig);

Console.WriteLine("[Datadog] APM Trace enabled");

// Configure and register the Datadog Tracer for distributed tracing
var tracer = DatadogTracing.NewTracerBuilder(Datadog.Android.Datadog.Instance)
    .WithServiceName(DatadogConfig.ServiceName)
    .SetBundleWithRumEnabled(true)  // Link traces with RUM
    .Build();

GlobalDatadogTracer.RegisterIfAbsent(tracer);

Console.WriteLine("[Datadog] Distributed tracing configured");
```

### Using Traces in Code

#### Manual Tracing

```csharp
using Datadog.Android.Trace;

// Get the global tracer
var tracer = GlobalDatadogTracer.Get();

// Create a span
var span = tracer.BuildSpan("operation_name").Start();

try
{
    // Your code here
    DoSomething();
}
catch (Exception ex)
{
    span.LogThrowable(ex);
}
finally
{
    span.Finish();
}
```

#### Trace HTTP Requests

For `HttpClient` requests in [ApiService.cs](MauiApp/Services/ApiService.cs):

```csharp
var tracer = GlobalDatadogTracer.Get();
var span = tracer.BuildSpan("http_request")
    .WithTag("http.url", url)
    .WithTag("http.method", "POST")
    .Start();

try
{
    var response = await _httpClient.PostAsync(url, content);
    span.SetTag("http.status_code", (int)response.StatusCode);

    if (!response.IsSuccessStatusCode)
    {
        span.SetTag("error", true);
    }
}
catch (Exception ex)
{
    span.LogThrowable(ex);
    throw;
}
finally
{
    span.Finish();
}
```

### Verify in Datadog

1. Run the app and make API calls
2. Go to [Datadog APM](https://app.datadoghq.com/apm/traces)
3. See your mobile→backend traces

---

## How to Enable WebView Tracking

WebView tracking monitors web content loaded in WebViews.

### Step 1: Add Using Statements

In the file where you create WebViews (e.g., [WebPortalPage.xaml.cs](MauiApp/Pages/WebPortalPage.xaml.cs)), add:

```csharp
#if ANDROID
using Datadog.Android.WebView;
#endif
```

### Step 2: Enable Tracking for WebView

After creating your WebView:

```csharp
#if ANDROID
// Enable JavaScript (required for tracking)
if (webView.Handler?.PlatformView is Android.Webkit.WebView androidWebView)
{
    androidWebView.Settings.JavaScriptEnabled = true;

    // Enable Datadog tracking for specific hosts
    var allowedHosts = new[] { "localhost", "10.0.2.2", "example.com" };
    WebViewTracking.Enable(androidWebView, allowedHosts);

    Console.WriteLine("[Datadog] WebView tracking enabled");
}
#endif
```

### Example: Update WebPortalPage

In [MauiApp/Pages/WebPortalPage.xaml.cs](MauiApp/Pages/WebPortalPage.xaml.cs):

```csharp
using DatadogMauiApp.Services;
#if ANDROID
using Datadog.Android.WebView;
#endif

namespace DatadogMauiApp.Pages;

public partial class WebPortalPage : ContentPage
{
    private readonly ApiService _apiService;

    public WebPortalPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        LoadWebViewUrl();
        EnableDatadogTracking();
    }

    private void EnableDatadogTracking()
    {
#if ANDROID
        if (WebViewControl.Handler?.PlatformView is Android.Webkit.WebView androidWebView)
        {
            androidWebView.Settings.JavaScriptEnabled = true;
            var allowedHosts = new[] { "localhost", "10.0.2.2" };
            WebViewTracking.Enable(androidWebView, allowedHosts);
            Console.WriteLine("[Datadog] WebView tracking enabled for web portal");
        }
#endif
    }

    private async void LoadWebViewUrl()
    {
        try
        {
            var config = await _apiService.GetConfigAsync();
            if (config != null && !string.IsNullOrEmpty(config.WebViewUrl))
            {
                WebViewControl.Source = config.WebViewUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] Failed to load config: {ex.Message}");
        }
    }

    private void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
    {
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (e.Result == WebNavigationResult.Success)
        {
            Console.WriteLine($"[Telemetry] WebView Navigated: {e.Url}");
        }
        else
        {
            Console.WriteLine($"[Telemetry] WebView Navigation Failed: {e.Url}");
        }
    }
}
```

### Allowed Hosts

The `allowedHosts` parameter matches hosts and their subdomains:
- `"example.com"` matches `example.com`, `www.example.com`, `api.example.com`
- No regular expressions allowed
- For local development, use: `new[] { "localhost", "10.0.2.2", "127.0.0.1" }`

### Verify in Datadog

1. Navigate to WebView pages in the app
2. Go to [Datadog RUM Explorer](https://app.datadoghq.com/rum/explorer)
3. See WebView resources and actions

---

## Complete Example

Here's what the full [MainApplication.cs](MauiApp/Platforms/Android/MainApplication.cs) looks like with all features enabled:

```csharp
using Android.App;
using Android.Runtime;
using Datadog.Android.Core.Configuration;
using Datadog.Android.Log;
using Datadog.Android.Ndk;
using Datadog.Android.Privacy;
using Datadog.Android.Rum;
using Datadog.Android.SessionReplay;  // Add this
using Datadog.Android.Trace;          // Add this
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
            Console.WriteLine($"[Datadog] - Client Token: {DatadogConfig.ClientToken.Substring(0, 10)}...");
            Console.WriteLine($"[Datadog] - RUM Application ID: {DatadogConfig.RumApplicationId}");

            // Core SDK
            var config = new DDConfiguration.Builder(
                DatadogConfig.ClientToken,
                DatadogConfig.Environment,
                string.Empty,
                DatadogConfig.ServiceName
            ).Build();

            Datadog.Android.Datadog.Initialize(this, config, TrackingConsent.Granted);
            Console.WriteLine("[Datadog] Core SDK initialized");

            if (DatadogConfig.VerboseLogging)
            {
                Datadog.Android.Datadog.Verbosity = (int)Android.Util.LogPriority.Verbose;
            }

            // Logs
            var logsConfig = new LogsConfiguration.Builder().Build();
            Logs.Enable(logsConfig);
            Console.WriteLine("[Datadog] Logs enabled");

            // NDK Crash Reports
            NdkCrashReports.Enable();
            Console.WriteLine("[Datadog] NDK crash reports enabled");

            // RUM
            var rumConfiguration = new RumConfiguration.Builder(DatadogConfig.RumApplicationId)
                .TrackLongTasks()
                .TrackFrustrations(true)
                .TrackBackgroundEvents(true)
                .TrackNonFatalAnrs(true)
                .Build();

            Datadog.Android.Rum.Rum.Enable(rumConfiguration);
            Console.WriteLine("[Datadog] RUM enabled");

            _ = Datadog.Android.Rum.GlobalRumMonitor.Instance;
            _ = Datadog.Android.Rum.GlobalRumMonitor.Get();

            // APM Tracing
            var traceConfig = new TraceConfiguration.Builder().Build();
            Datadog.Android.Trace.Trace.Enable(traceConfig);

            var tracer = DatadogTracing.NewTracerBuilder(Datadog.Android.Datadog.Instance)
                .WithServiceName(DatadogConfig.ServiceName)
                .SetBundleWithRumEnabled(true)
                .Build();

            GlobalDatadogTracer.RegisterIfAbsent(tracer);
            Console.WriteLine("[Datadog] APM Tracing enabled");

            // Session Replay
            var sessionReplayConfig = new SessionReplayConfiguration.Builder(
                new[] { DatadogConfig.SessionReplaySampleRate }
            )
                .SetPrivacy(new SessionReplayPrivacy(
                    TextAndInputPrivacy.MaskAll,
                    ImagePrivacy.MaskAll,
                    TouchPrivacy.Hide
                ))
                .Build();

            SessionReplay.Enable(sessionReplayConfig);
            SessionReplay.Instance?.StartRecording();
            Console.WriteLine("[Datadog] Session Replay enabled and recording");

            Console.WriteLine("[Datadog] Successfully initialized with all features");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Datadog] Failed to initialize: {ex.Message}");
            Console.WriteLine($"[Datadog] Stack trace: {ex.StackTrace}");
        }
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
```

---

## Performance Impact

| Feature | CPU Impact | Memory Impact | Network Impact |
|---------|------------|---------------|----------------|
| RUM | <1% | Low | Minimal |
| Logs | <1% | Low | Minimal |
| APM Tracing | 1-2% | Low | Minimal |
| Session Replay | 2-5% | Medium | Moderate |
| WebView Tracking | <1% | Low | Minimal |

**Recommendations:**
- Enable all features in development with 100% sample rates
- In production, use 100% for RUM, 20-50% for Session Replay
- Monitor Datadog costs and adjust sample rates accordingly

---

## Troubleshooting

### Session Replay Not Recording

1. Check sample rate is > 0
2. Verify RUM is enabled first
3. Check console for Session Replay logs
4. Wait 1-2 minutes for data to appear in dashboard

### Traces Not Appearing

1. Verify `TrackingConsent.Granted` is set
2. Check that `SetBundleWithRumEnabled(true)` is called
3. Ensure spans are properly finished
4. Check Datadog APM dashboard filters

### WebView Tracking Not Working

1. Verify JavaScript is enabled on WebView
2. Check that host is in `allowedHosts` array
3. Ensure WebView has loaded content
4. Check browser console for errors

---

## Summary

To enable all advanced features:

1. ✅ Packages already installed in csproj
2. Add using statements for desired features
3. Copy initialization code from this guide
4. Test in Datadog dashboard

**Current Status:** Core features enabled, advanced features ready to enable when needed.
