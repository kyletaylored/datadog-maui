# Datadog Integration Setup Guide - Updated

## Overview

This guide explains the updated Datadog integration approach for your MAUI app. We're now using **NuGet packages** instead of local project references.

---

## Current Status

### ✅ Complete - Backend API (.NET 9.0)
The backend API Datadog integration is fully functional. See [API_DATADOG_SETUP.md](API_DATADOG_SETUP.md) for details.

### ⚠️ In Progress - Mobile App (MAUI .NET 10.0)

The mobile app is configured to use Datadog NuGet packages:
- ✅ NuGet packages installed (Android: v2.21.0-pre.1, iOS: v2.26.0)
- ✅ Android app builds successfully
- ⚠️ iOS build requires Xcode/simulator runtime updates
- ⚠️ Datadog initialization code temporarily disabled (needs API updates)

---

## What's Been Done

### 1. NuGet Packages Added

[MauiApp/DatadogMauiApp.csproj](MauiApp/DatadogMauiApp.csproj) now includes:

**Android Packages** (v2.21.0-pre.1 - latest available):
- `Bcr.Datadog.Android.Sdk.Core`
- `Bcr.Datadog.Android.Sdk.Logs`
- `Bcr.Datadog.Android.Sdk.Rum`
- `Bcr.Datadog.Android.Sdk.SessionReplay`
- `Bcr.Datadog.Android.Sdk.SessionReplay.Material`
- `Bcr.Datadog.Android.Sdk.Trace`
- `Bcr.Datadog.Android.Sdk.Ndk`

**iOS Packages** (v2.26.0 - latest available):
- `Bcr.Datadog.iOS.Core`
- `Bcr.Datadog.iOS.Logs`
- `Bcr.Datadog.iOS.RUM`
- `Bcr.Datadog.iOS.Trace`
- `Bcr.Datadog.iOS.ObjC`

**Note**: Android and iOS packages are at different versions because the package maintainer has published newer versions for iOS but not Android yet.

### 2. Configuration Class Created

[MauiApp/Config/DatadogConfig.cs](MauiApp/Config/DatadogConfig.cs) contains placeholders for:
- Client Token
- RUM Application ID
- Environment settings
- Sample rates

---

## Next Steps

### Step 1: Update Datadog Initialization Code ⚠️

The Datadog initialization has been **temporarily disabled** in:
- [MauiApp/Platforms/Android/MainApplication.cs](MauiApp/Platforms/Android/MainApplication.cs:31) - commented out, needs update for v2.21.0-pre.1 API
- [MauiApp/Platforms/iOS/AppDelegate.cs](MauiApp/Platforms/iOS/AppDelegate.cs:22) - commented out, needs verification for v2.26.0 API

**Current Status**:
- ✅ Android app builds successfully (initialization disabled)
- ⚠️ iOS app has Xcode/simulator runtime version issues (unrelated to Datadog)
- ⚠️ Datadog is NOT currently initializing in the app

**To complete the integration**:
1. Refer to the [datadog-dotnet-mobile-sdk-bindings repository](https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings)
2. Check the `samples` directory for initialization examples
3. Update the commented-out code in both platform files
4. Uncomment the `InitializeDatadog()` calls
5. Test that initialization works correctly

**Key API changes for Android (v2.21.0-pre.1)**:
- `SessionReplayConfiguration.Builder` constructor signature changed
- `SessionReplayPrivacy` constructor changed
- `SessionReplay.Enable` method signature changed
- `ExtensionSupport` class location/namespace changed

### Step 2: Get Datadog Credentials

1. Go to [Datadog Organization Settings](https://app.datadoghq.com/organization-settings/client-tokens)
2. Get your **Client Token**
3. Go to [RUM Applications](https://app.datadoghq.com/rum/list)
4. Get your **RUM Application ID**
5. Update [MauiApp/Config/DatadogConfig.cs](MauiApp/Config/DatadogConfig.cs):

```csharp
public const string ClientToken = "YOUR_ACTUAL_CLIENT_TOKEN";
public const string RumApplicationId = "YOUR_ACTUAL_RUM_APP_ID";
public const string Site = "us1"; // or eu1, us3, us5, ap1
```

### Step 3: Build the App

✅ **Android Build - Working**:
```bash
cd MauiApp
dotnet restore  # Already done
dotnet build -f net10.0-android  # ✅ Builds successfully
```

⚠️ **iOS Build - Requires Xcode Updates**:
```bash
dotnet build -f net10.0-ios  # ❌ Fails due to simulator runtime version mismatch
```

Error: `No simulator runtime version from ["21F79", "22C150", "22E238", "23B80"] available to use with iphonesimulator SDK version 23C53`

This is an Xcode/iOS simulator configuration issue, not related to Datadog. To fix:
1. Update Xcode to the latest version
2. Install required iOS simulator runtimes via Xcode → Settings → Platforms
3. Or use `xcodebuild -version` and `xcode-select` to verify/update SDK paths

### Step 4: Update Initialization Code

Once the app builds successfully, refer to the binding repository for examples of the correct initialization pattern:

1. Check the [samples directory](https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings/tree/main/samples) in the repository
2. Look for initialization examples for v2.21.0-pre.1
3. Update your `InitializeDatadog()` methods accordingly

---

## Why NuGet Packages Instead of Project References?

**Previous Approach**: We tried to reference the binding projects directly from the cloned repository.

**Problems**:
- Version mismatches between .NET SDK and binding targets
- EOL framework warnings (net8.0-ios17.0)
- API compatibility issues
- Complex build dependencies

**New Approach**: Use published NuGet packages.

**Benefits**:
- ✅ Simpler dependency management
- ✅ No need to build bindings manually
- ✅ Automatic restoration with `dotnet restore`
- ✅ Version pinning for stability
- ✅ Works across different .NET SDK versions

---

## Package Versions

**Current Versions**:
- Android: 2.21.0-pre.1 (latest available on NuGet)
- iOS: 2.26.0 (latest available on NuGet)

The package maintainer has published newer versions for iOS but not for Android yet. Version 2.26.0 exists in the GitHub repository for Android but hasn't been published to NuGet.

**To check for newer versions**:
```bash
dotnet package search Bcr.Datadog.Android.Sdk.Core --prerelease
dotnet package search Bcr.Datadog.iOS.Core --prerelease
```

**To update to a newer version** (when available):
1. Update the version numbers in [MauiApp/DatadogMauiApp.csproj](MauiApp/DatadogMauiApp.csproj)
2. Run `dotnet restore`
3. Update initialization code if API changed

---

## Reference Repository

The [datadog-dotnet-mobile-sdk-bindings](https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings) repository is now only used for:
- ✅ API documentation
- ✅ Code examples
- ✅ Understanding the binding structure
- ✅ Checking for new releases

You **do not need** to:
- ❌ Clone the repository
- ❌ Build the bindings
- ❌ Reference the projects directly

---

## Backend API Integration

The backend API Datadog integration is **complete and working**. See [API_DATADOG_SETUP.md](API_DATADOG_SETUP.md) for:
- Starting the Datadog Agent
- Testing API endpoints
- Viewing traces in Datadog
- RUM-to-APM correlation setup

---

## Troubleshooting

### Build Errors After Adding Packages

**Issue**: Compilation errors in MainApplication.cs or AppDelegate.cs

**Solution**: Remove or comment out the `InitializeDatadog()` method calls until you can update the code for the v2.21.0-pre.1 API.

### Package Restore Fails

**Issue**: Unable to find Datadog packages

**Solution**:
1. Make sure you're using prerelease versions (2.21.0-pre.1)
2. Enable prerelease packages: `dotnet restore --prerelease`
3. Or add `<PackageReference ... />` with the `-pre.1` suffix

### Wrong API Being Used

**Issue**: Methods or constructors don't exist

**Solution**: The API differs between versions. Check the repository's samples folder for v2.21.0-pre.1 examples.

---

## Summary

### Current State:
- ✅ Backend API: Datadog fully integrated and working
- ✅ Mobile App NuGet Packages: Installed (Android: v2.21.0-pre.1, iOS: v2.26.0)
- ✅ Android Build: Building successfully
- ⚠️ iOS Build: Requires Xcode/simulator runtime updates
- ⚠️ Datadog Initialization: Temporarily disabled, needs API updates
- ✅ Configuration: DatadogConfig.cs ready for credentials
- ✅ Documentation: Complete setup guides available

### To Complete Mobile Integration:
1. ✅ ~~Install NuGet packages~~ - Done
2. ✅ ~~Get app building~~ - Done (Android)
3. ⚠️ Update Xcode/iOS simulators (for iOS build)
4. ⚠️ Get Datadog credentials (Client Token + RUM Application ID)
5. ⚠️ Update initialization code for current API versions
   - Android: Update for v2.21.0-pre.1 API
   - iOS: Verify/update for v2.26.0 API
6. ⚠️ Uncomment `InitializeDatadog()` calls
7. ⚠️ Test RUM data flowing to Datadog

### Files to Update:
- [MauiApp/Config/DatadogConfig.cs](MauiApp/Config/DatadogConfig.cs) - Add credentials
- [MauiApp/Platforms/Android/MainApplication.cs](MauiApp/Platforms/Android/MainApplication.cs) - Update initialization
- [MauiApp/Platforms/iOS/AppDelegate.cs](MauiApp/Platforms/iOS/AppDelegate.cs) - Update initialization

---

## Resources

- [Datadog RUM Documentation](https://docs.datadoghq.com/real_user_monitoring/)
- [Bindings Repository](https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings)
- [NuGet Packages](https://www.nuget.org/packages?q=Bcr.Datadog)
- [Backend API Setup](API_DATADOG_SETUP.md)
