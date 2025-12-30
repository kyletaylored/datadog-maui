# Datadog Integration Setup Guide

## Overview

This guide explains how to complete the Datadog integration for your MAUI app using the community bindings from [brunck/datadog-dotnet-mobile-sdk-bindings](https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings).

---

## Prerequisites

✅ Datadog account - Sign up at [datadoghq.com](https://www.datadoghq.com/)
✅ Client Token from Datadog
✅ RUM Application ID from Datadog
✅ Datadog binding repository cloned locally

---

## Step 1: Build Datadog Bindings

The Datadog bindings need to be built before they can be used.

### Build Android Bindings

```bash
cd datadog-dotnet-mobile-sdk-bindings/src/Android/Bindings

# Build all Android bindings
dotnet build Core/Core.csproj
dotnet build DatadogLogs/DatadogLogs.csproj
dotnet build Rum/Rum.csproj
dotnet build SessionReplay/SessionReplay.csproj
dotnet build SessionReplay.Material/SessionReplay.Material.csproj
dotnet build Trace/Trace.csproj
dotnet build Ndk/Ndk.csproj
```

### Build iOS Bindings

```bash
cd datadog-dotnet-mobile-sdk-bindings/src/iOS/Bindings

# Build all iOS bindings
dotnet build Core/Core.csproj
dotnet build DDLogs/DDLogs.csproj
dotnet build Rum/Rum.csproj
dotnet build SessionReplay/SessionReplay.csproj
dotnet build Trace/Trace.csproj
dotnet build CrashReporting/CrashReporting.csproj
dotnet build ObjC/ObjC.csproj
```

---

## Step 2: Get Datadog Credentials

### Get Client Token

1. Go to [Datadog Organization Settings](https://app.datadoghq.com/organization-settings/client-tokens)
2. Click "New Client Token" or copy an existing one
3. Save this token - you'll need it for configuration

### Get RUM Application ID

1. Go to [RUM Applications](https://app.datadoghq.com/rum/list)
2. Click "New Application" if you don't have one
3. Select "iOS" or "Android" as the application type
4. Copy the "Application ID"
5. Save this ID - you'll need it for configuration

### Determine Your Datadog Site

Your Datadog site depends on your region:
- **US1**: `https://app.datadoghq.com` (most common)
- **EU1**: `https://app.datadoghq.eu`
- **US3**: `https://us3.datadoghq.com`
- **US5**: `https://us5.datadoghq.com`
- **AP1**: `https://ap1.datadoghq.com`

---

## Step 3: Configure Datadog Credentials

Edit [MauiApp/Config/DatadogConfig.cs](MauiApp/Config/DatadogConfig.cs):

```csharp
public static class DatadogConfig
{
    // Replace with your actual Client Token
    public const string ClientToken = "pub1234567890abcdef1234567890abcd";

    // Replace with your actual RUM Application ID
    public const string RumApplicationId = "12345678-1234-1234-1234-123456789012";

    // Set your environment
    public const string Environment = "dev"; // or "staging", "prod"

    // Set your service name
    public const string ServiceName = "datadog-maui-app";

    // Set your Datadog site
    public const string Site = "us1"; // or "eu1", "us3", "us5", "ap1"

    // Adjust sample rates as needed (0-100)
    public const float SessionSampleRate = 100f;
    public const float SessionReplaySampleRate = 100f;

    // Enable/disable verbose logging
    public const bool VerboseLogging = true;
}
```

---

## Step 4: What's Already Configured

The following files have been updated with Datadog integration:

### ✅ Project References Added

[MauiApp/DatadogMauiApp.csproj](MauiApp/DatadogMauiApp.csproj) now includes:
- Android-specific Datadog bindings (Core, Logs, RUM, SessionReplay, Trace, Ndk)
- iOS-specific Datadog bindings (Core, Logs, RUM, SessionReplay, Trace, CrashReporting)

### ✅ Android Initialization

[MauiApp/Platforms/Android/MainApplication.cs](MauiApp/Platforms/Android/MainApplication.cs) includes:
- Datadog SDK initialization
- Logs configuration
- RUM (Real User Monitoring) with:
  - Long task tracking
  - Frustration tracking
  - Background events
  - ANR (App Not Responding) detection
- **Session Replay** with privacy settings:
  - Text and input masking
  - Image masking
  - Touch privacy
- NDK crash reports
- Material extension support

### ✅ iOS Initialization

[MauiApp/Platforms/iOS/AppDelegate.cs](MauiApp/Platforms/iOS/AppDelegate.cs) includes:
- Datadog SDK initialization
- Logs configuration
- Crash reporting
- RUM (Real User Monitoring)
- **Session Replay** with privacy settings:
  - Text and input masking
  - Image masking
  - Touch privacy
- Trace configuration

---

## Step 5: Build the App

Once the bindings are built and credentials are configured:

### Android
```bash
cd MauiApp
dotnet restore
dotnet build -f net10.0-android
```

### iOS
```bash
cd MauiApp
dotnet restore
dotnet build -f net10.0-ios
```

---

## Step 6: Verify Datadog Integration

### Check Console Logs

When the app starts, you should see:
```
[Datadog] Successfully initialized for Android
```
or
```
[Datadog] Successfully initialized for iOS
```

### Check Datadog Dashboard

1. Go to [RUM Explorer](https://app.datadoghq.com/rum/explorer)
2. Wait 1-2 minutes after launching the app
3. You should see sessions appearing
4. Click on a session to see:
   - Session details
   - Views (pages visited)
   - Actions (button clicks, gestures)
   - Resources (API calls)
   - Errors (if any)
   - **Session Replays** (video playback of user session)

### Check Session Replay

1. Go to [Session Replay](https://app.datadoghq.com/rum/replay/sessions)
2. Find your session
3. Click to watch the replay
4. You'll see screen recordings with privacy masking applied

---

## Features Enabled

### ✅ Real User Monitoring (RUM)
- **Views**: Automatically tracks page navigation
- **Actions**: Tracks taps, swipes, and gestures
- **Resources**: Tracks API calls and network requests
- **Errors**: Captures crashes and errors
- **Long Tasks**: Detects UI freezes
- **Frustrations**: Detects rage taps and error taps
- **ANRs**: Detects app not responding on Android

### ✅ Session Replay
- **Screen Recording**: Records user sessions
- **Privacy Protection**:
  - All text masked by default
  - All images masked by default
  - Touch interactions hidden
- **Playback**: View sessions in Datadog dashboard

### ✅ Logs
- Application logs sent to Datadog
- Structured logging with metadata
- Correlation with RUM sessions

### ✅ Crash Reporting
- Automatic crash capture
- Stack traces sent to Datadog
- Crash analytics in dashboard

### ✅ Traces (APM)
- Distributed tracing ready
- Can trace API calls end-to-end
- Performance monitoring

---

## Optional: Add Manual RUM Events

You can manually track events in your code:

### Track Custom Actions

```csharp
// In any .cs file
#if ANDROID
using Datadog.Android.Rum;
var rumMonitor = GlobalRumMonitor.Get();
rumMonitor?.AddAction(
    RumActionType.Tap,
    "button_submit",
    new Dictionary<string, Java.Lang.Object>()
);
#elif IOS
using Datadog.iOS.ObjC;
DDRUMMonitor.Shared()?.AddAction(
    DDRUMActionType.Tap,
    "button_submit",
    new Foundation.NSDictionary()
);
#endif
```

### Track Custom Errors

```csharp
#if ANDROID
rumMonitor?.AddError("Custom error message", RumErrorSource.Source, exception, attributes);
#elif IOS
DDRUMMonitor.Shared()?.AddError("Custom error message", DDRUMErrorSource.Source, exception, attributes);
#endif
```

### Track Custom Resources (API Calls)

This can be added to [ApiService.cs](MauiApp/Services/ApiService.cs):

```csharp
#if ANDROID
using Datadog.Android.Rum;

// Before API call
var rumMonitor = GlobalRumMonitor.Get();
var resourceKey = Guid.NewGuid().ToString();
rumMonitor?.StartResource(resourceKey, "POST", url, new Dictionary<string, Java.Lang.Object>());

// After API call
if (response.IsSuccessStatusCode)
{
    rumMonitor?.StopResource(resourceKey, (int)response.StatusCode,
        response.Content.Headers.ContentLength ?? 0,
        RumResourceType.Native, new Dictionary<string, Java.Lang.Object>());
}
#elif IOS
// Similar for iOS using DDRUMMonitor
#endif
```

---

## Privacy Settings

### Current Configuration

**Text and Input**: `MaskAll` - All text fields are masked
**Images**: `MaskAll` - All images are masked
**Touch**: `Hide` - Touch interactions are hidden

### Adjust Privacy Settings

Edit [MainApplication.cs:72-75](MauiApp/Platforms/Android/MainApplication.cs#L72-L75) or [AppDelegate.cs:53-58](MauiApp/Platforms/iOS/AppDelegate.cs#L53-L58):

```csharp
// Android
TextAndInputPrivacy.MaskAll        // or .MaskSensitive, .Allow
ImagePrivacy.MaskAll               // or .MaskNone, .MaskLarge
TouchPrivacy.Hide                  // or .Show

// iOS
DDTextAndInputPrivacyLevel.MaskAll // or .MaskSensitiveInputs, .AllowAll
DDImagePrivacyLevel.MaskAll        // or .MaskNone, .MaskNonBundledOnly
DDTouchPrivacyLevel.Hide           // or .Show
```

---

## Sample Rates

### Adjust in DatadogConfig.cs

```csharp
// 100 = track all sessions (good for development)
public const float SessionSampleRate = 100f;

// 20 = replay only 20% of sessions (good for production)
public const float SessionReplaySampleRate = 20f;
```

**Recommendation**:
- **Development**: 100% for both
- **Production**: 100% for RUM, 20-50% for Session Replay (to reduce costs)

---

## Troubleshooting

### Bindings Don't Build

**Issue**: `The type or namespace name 'Datadog' could not be found`

**Solution**: Build the binding projects first (see Step 1)

```bash
# Navigate to bindings repo
cd datadog-dotnet-mobile-sdk-bindings

# Build all bindings
dotnet build src/Android/Bindings/Android.Bindings.sln
dotnet build src/iOS/Bindings/iOS.Bindings.sln
```

### No Data in Datadog Dashboard

**Check**:
1. ✅ Client Token is correct
2. ✅ RUM Application ID is correct
3. ✅ Site is correct (us1, eu1, etc.)
4. ✅ App has network access
5. ✅ Wait 1-2 minutes for data to appear

**Enable verbose logging** to see what's happening:
```csharp
public const bool VerboseLogging = true;
```

### Session Replay Not Working

**Check**:
1. ✅ Session Replay is enabled in code
2. ✅ Sample rate is > 0
3. ✅ RUM is properly initialized
4. ✅ App has necessary permissions

### iOS Build Issues

**Issue**: Missing xcframework files

**Solution**: The iOS bindings depend on Datadog native frameworks. Make sure the bindings were built successfully.

---

## Performance Impact

### Minimal Overhead

- **RUM**: < 1% CPU impact
- **Session Replay**: 2-5% CPU impact (during recording)
- **Network**: Small data upload in background
- **Battery**: Minimal impact (< 1%)

### Best Practices

1. ✅ Use appropriate sample rates in production
2. ✅ Enable Session Replay conditionally
3. ✅ Monitor Datadog costs
4. ✅ Adjust privacy settings as needed

---

## Next Steps

1. **Build Bindings** (Step 1)
2. **Get Credentials** (Step 2)
3. **Configure** (Step 3)
4. **Build App** (Step 5)
5. **Test** (Step 6)
6. **Monitor Dashboard**

---

## Resources

- [Datadog RUM Documentation](https://docs.datadoghq.com/real_user_monitoring/)
- [Datadog Session Replay](https://docs.datadoghq.com/real_user_monitoring/session_replay/)
- [Community Bindings Repo](https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings)
- [Datadog Android SDK](https://github.com/DataDog/dd-sdk-android)
- [Datadog iOS SDK](https://github.com/DataDog/dd-sdk-ios)

---

## Summary

✅ **Project References**: Added to csproj
✅ **Android Init**: Complete with Session Replay
✅ **iOS Init**: Complete with Session Replay
✅ **Configuration**: DatadogConfig.cs created
✅ **Privacy**: Masking enabled by default
✅ **Documentation**: This guide

**Status**: Ready to build once bindings are compiled and credentials are configured!
