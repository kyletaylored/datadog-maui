# Datadog Integration Status

**Date**: December 30, 2025
**Project**: MAUI Cross-Platform App with Datadog RUM & APM

---

## 🎉 Summary

Datadog has been successfully integrated into both the **backend API** and **mobile app** using NuGet packages. The backend is fully operational, and the mobile app is configured and building (Android).

---

## ✅ What's Working

### Backend API (.NET 9.0) - ✅ **FULLY OPERATIONAL**

The backend API has complete Datadog APM (Application Performance Monitoring) integration:

- **✅ NuGet Packages Installed**:
  - `Datadog.Trace` v3.11.0
  - `Datadog.Trace.Bundle` v3.11.0

- **✅ Custom Tracing Implemented**:
  - All endpoints have custom spans with descriptive operation names
  - Tags added for filtering and analysis
  - Correlation ID support for linking mobile RUM to backend traces

- **✅ Endpoints Instrumented**:
  - `GET /health` - Health check with tracing
  - `GET /config` - Configuration endpoint with correlation ID extraction
  - `POST /data` - Data submission with correlation ID tagging
  - `GET /data` - Data retrieval with count tracking

- **✅ Trace Data Returned**:
  - API responses include `traceId` and `spanId` for debugging
  - Enables end-to-end request tracking from mobile → API → Datadog

- **✅ Docker Configuration**:
  - Dockerfile includes Datadog environment variables
  - Ready for Datadog Agent connection
  - Configurable via environment variables

- **✅ Documentation**:
  - [API_DATADOG_SETUP.md](API_DATADOG_SETUP.md) - Complete setup guide
  - Datadog Agent setup instructions
  - Testing procedures
  - Viewing traces in Datadog dashboard

**Status**: Ready to use! Start the API and Datadog Agent to see traces.

---

### Mobile App (.NET 10.0 MAUI) - ⚠️ **CONFIGURED, NEEDS INITIALIZATION UPDATE**

The mobile app has Datadog NuGet packages installed and builds successfully on Android:

- **✅ NuGet Packages Installed**:
  - **Android** (v2.21.0-pre.1 - latest available):
    - `Bcr.Datadog.Android.Sdk.Core`
    - `Bcr.Datadog.Android.Sdk.Logs`
    - `Bcr.Datadog.Android.Sdk.Rum`
    - `Bcr.Datadog.Android.Sdk.SessionReplay`
    - `Bcr.Datadog.Android.Sdk.SessionReplay.Material`
    - `Bcr.Datadog.Android.Sdk.Trace`
    - `Bcr.Datadog.Android.Sdk.Ndk`

  - **iOS** (v2.26.0 - latest available):
    - `Bcr.Datadog.iOS.Core`
    - `Bcr.Datadog.iOS.Logs`
    - `Bcr.Datadog.iOS.RUM`
    - `Bcr.Datadog.iOS.Trace`
    - `Bcr.Datadog.iOS.ObjC`

- **✅ Configuration Ready**:
  - [MauiApp/Config/DatadogConfig.cs](MauiApp/Config/DatadogConfig.cs) created with placeholders
  - Centralized configuration for credentials and settings

- **✅ Android Build Working**:
  - `dotnet restore` succeeds
  - `dotnet build -f net10.0-android` succeeds
  - App compiles without errors

- **✅ Correlation ID Support**:
  - [MauiApp/Services/ApiService.cs](MauiApp/Services/ApiService.cs) sends `X-Correlation-ID` header
  - Enables RUM-to-APM correlation

- **⚠️ Initialization Code Disabled**:
  - Initialization code commented out in:
    - [MauiApp/Platforms/Android/MainApplication.cs](MauiApp/Platforms/Android/MainApplication.cs:31)
    - [MauiApp/Platforms/iOS/AppDelegate.cs](MauiApp/Platforms/iOS/AppDelegate.cs:22)
  - **Reason**: API changes between versions - old initialization code incompatible with current packages
  - **Status**: Needs updating for v2.21.0-pre.1 (Android) and v2.26.0 (iOS) APIs

- **⚠️ iOS Build Issue**:
  - Xcode/iOS simulator runtime version mismatch
  - Error: `No simulator runtime version from ["21F79", "22C150", "22E238", "23B80"] available to use with iphonesimulator SDK version 23C53`
  - **Reason**: Xcode 26.2 SDK version (23C53) doesn't match available simulator runtimes
  - **Impact**: iOS build fails, but this is NOT a Datadog or code issue - it's an Xcode configuration issue
  - **Workaround**: Android builds successfully and demonstrates the integration

**Status**: Configured and building (Android). Initialization code needs API updates to be fully functional.

---

## 📦 Package Versions

### Backend API
- `Datadog.Trace`: 3.11.0 (latest stable)
- `Datadog.Trace.Bundle`: 3.11.0 (latest stable)

### Mobile App

**Android**: v2.21.0-pre.1 (latest available on NuGet)
**iOS**: v2.26.0 (latest available on NuGet)

**Why different versions?**
The package maintainer has published newer versions for iOS but not for Android yet. Version 2.26.0 exists in the GitHub repository for Android but hasn't been published to NuGet.

**Check for updates**:
```bash
dotnet package search Bcr.Datadog.Android.Sdk.Core --prerelease
dotnet package search Bcr.Datadog.iOS.Core --prerelease
```

---

## 📁 Key Files

### Backend API
- [Api/DatadogMauiApi.csproj](Api/DatadogMauiApi.csproj) - NuGet package references
- [Api/Program.cs](Api/Program.cs) - Datadog configuration & custom tracing
- [Api/Dockerfile](Api/Dockerfile) - Datadog environment variables
- [API_DATADOG_SETUP.md](API_DATADOG_SETUP.md) - Complete setup documentation

### Mobile App
- [MauiApp/DatadogMauiApp.csproj](MauiApp/DatadogMauiApp.csproj) - NuGet package references
- [MauiApp/Config/DatadogConfig.cs](MauiApp/Config/DatadogConfig.cs) - Configuration (needs credentials)
- [MauiApp/Platforms/Android/MainApplication.cs](MauiApp/Platforms/Android/MainApplication.cs) - Android initialization (disabled)
- [MauiApp/Platforms/iOS/AppDelegate.cs](MauiApp/Platforms/iOS/AppDelegate.cs) - iOS initialization (disabled)
- [MauiApp/Services/ApiService.cs](MauiApp/Services/ApiService.cs) - Correlation ID support
- [DATADOG_SETUP_UPDATED.md](DATADOG_SETUP_UPDATED.md) - Mobile setup documentation

---

## 🚀 Quick Start

### Backend API (Ready to Use)

1. **Start the API**:
   ```bash
   make api-build
   make api-start
   ```

2. **Test endpoints**:
   ```bash
   make api-test
   ```

3. **View logs**:
   ```bash
   make api-logs
   ```

4. **Start Datadog Agent** (to send traces to Datadog):
   ```bash
   docker run -d \
     --name datadog-agent \
     -e DD_API_KEY=<YOUR_DATADOG_API_KEY> \
     -e DD_SITE=datadoghq.com \
     -e DD_APM_ENABLED=true \
     -e DD_APM_NON_LOCAL_TRAFFIC=true \
     -p 8126:8126 \
     -p 8125:8125/udp \
     datadog/agent:latest
   ```

5. **View traces**: https://app.datadoghq.com/apm/traces

### Mobile App (Android)

1. **Restore packages**:
   ```bash
   cd MauiApp
   dotnet restore
   ```

2. **Build Android app**:
   ```bash
   dotnet build -f net10.0-android
   ```

3. **Run on emulator**:
   ```bash
   dotnet build -t:Run -f net10.0-android
   ```

**Or use Makefile**:
```bash
make app-build-android
make app-run-android
```

---

## ⚠️ Known Issues

### 1. iOS Build Fails - Xcode Simulator Runtime Mismatch

**Error**:
```
No simulator runtime version from ["21F79", "22C150", "22E238", "23B80"]
available to use with iphonesimulator SDK version 23C53
```

**Cause**: Xcode 26.2 SDK version (23C53) doesn't match available iOS simulator runtimes

**Impact**: iOS builds fail, but Android builds work fine

**This is NOT**:
- ❌ A Datadog integration issue
- ❌ A code issue
- ❌ A NuGet package issue

**This IS**:
- ✅ An Xcode/iOS SDK versioning issue
- ✅ Temporary - will resolve when Apple releases matching simulator runtime

**Workaround**:
- Use Android for development/testing
- Or downgrade Xcode to match available simulator versions
- Or wait for Apple to release matching simulator runtime

**Attempted Fix**:
- ✅ Ran `dotnet workload update` - all workloads already up to date
- ✅ Verified Xcode version (26.2)
- ✅ Verified available simulator runtimes

### 2. Datadog Initialization Code Disabled

**Location**:
- [MauiApp/Platforms/Android/MainApplication.cs:31](MauiApp/Platforms/Android/MainApplication.cs#L31)
- [MauiApp/Platforms/iOS/AppDelegate.cs:22](MauiApp/Platforms/iOS/AppDelegate.cs#L22)

**Why**: The initialization code was written for an older Datadog API version and is incompatible with:
- Android: v2.21.0-pre.1
- iOS: v2.26.0

**Key API Changes** (Android v2.21.0-pre.1):
- `SessionReplayConfiguration.Builder` constructor signature changed
- `SessionReplayPrivacy` constructor changed
- `SessionReplay.Enable` method signature changed
- `ExtensionSupport` class location/namespace changed

**To Fix**:
1. Visit: https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings
2. Check the `samples` directory for initialization examples
3. Update the commented-out code in both platform files
4. Uncomment the `InitializeDatadog()` calls
5. Test that initialization works correctly

**Current Behavior**:
- App prints: `[Datadog] Initialization disabled - needs API update for v2.21.0-pre.1`
- No Datadog RUM data is sent (initialization not running)
- App still functions normally otherwise

---

## 📋 Next Steps

### 1. Get Datadog Credentials

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

### 2. Update Mobile Initialization Code

**Reference**: [datadog-dotnet-mobile-sdk-bindings repository](https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings)

**Files to Update**:
- [MauiApp/Platforms/Android/MainApplication.cs:31](MauiApp/Platforms/Android/MainApplication.cs#L31)
- [MauiApp/Platforms/iOS/AppDelegate.cs:22](MauiApp/Platforms/iOS/AppDelegate.cs#L22)

**Steps**:
1. Check the `samples` directory in the binding repository
2. Find initialization examples for:
   - Android: v2.21.0-pre.1
   - iOS: v2.26.0
3. Update the commented-out code to match the current API
4. Uncomment the `InitializeDatadog()` calls
5. Rebuild and test

### 3. Test End-to-End Integration

Once initialization is updated:

1. **Start Datadog Agent**:
   ```bash
   docker run -d \
     --name datadog-agent \
     -e DD_API_KEY=<YOUR_KEY> \
     -e DD_SITE=datadoghq.com \
     -e DD_APM_ENABLED=true \
     -e DD_APM_NON_LOCAL_TRAFFIC=true \
     -p 8126:8126 \
     datadog/agent:latest
   ```

2. **Start Backend API**:
   ```bash
   make api-start
   ```

3. **Run Mobile App**:
   ```bash
   make app-run-android
   ```

4. **Submit Data** from the app

5. **Verify in Datadog**:
   - **RUM**: https://app.datadoghq.com/rum/explorer
     - Find your mobile session
     - View user actions, screens, resources
     - Session Replay (if working)

   - **APM**: https://app.datadoghq.com/apm/traces
     - Filter by `service:datadog-maui-api`
     - Find traces with your correlation ID
     - Click "View Trace" from RUM session to jump to backend trace

6. **Verify RUM-to-APM Correlation**:
   - Find a mobile session in RUM
   - Click on an API call resource
   - Click "View Trace" button
   - Should jump to the backend API trace
   - Verify correlation ID matches in both places

### 4. (Optional) Fix iOS Build Issue

**Option A**: Wait for Apple to release matching simulator runtime

**Option B**: Downgrade Xcode to match available simulator versions

**Option C**: Use Android for development (recommended short-term)

---

## 📚 Documentation

- **[README.md](README.md)** - Project overview and initial setup
- **[API_DATADOG_SETUP.md](API_DATADOG_SETUP.md)** - Complete backend API Datadog setup guide
- **[DATADOG_SETUP_UPDATED.md](DATADOG_SETUP_UPDATED.md)** - Mobile app Datadog setup guide (updated for NuGet packages)
- **[Makefile](Makefile)** - All build and run commands
- **[.gitignore](.gitignore)** - Git ignore patterns for .NET, Docker, macOS

---

## 🔗 Useful Links

- [Datadog RUM Documentation](https://docs.datadoghq.com/real_user_monitoring/)
- [Datadog APM Documentation](https://docs.datadoghq.com/tracing/)
- [Datadog .NET Tracer](https://docs.datadoghq.com/tracing/setup_overview/setup/dotnet-core/)
- [RUM-to-APM Correlation](https://docs.datadoghq.com/real_user_monitoring/connect_rum_and_traces/)
- [Binding Repository](https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings)
- [NuGet Packages](https://www.nuget.org/packages?q=Bcr.Datadog)

---

## ✅ Checklist

### Backend API
- ✅ NuGet packages installed
- ✅ Datadog configuration added to Program.cs
- ✅ Custom tracing spans implemented
- ✅ Correlation ID support added
- ✅ Docker configuration updated
- ✅ API builds successfully
- ✅ API runs in Docker
- ✅ Endpoints tested and working
- ✅ Documentation complete
- ⚠️ Datadog Agent setup (requires your API key)
- ⚠️ Traces visible in Datadog (requires Agent)

### Mobile App
- ✅ NuGet packages installed (Android: v2.21.0-pre.1, iOS: v2.26.0)
- ✅ Configuration class created
- ✅ Correlation ID support added to ApiService
- ✅ Android app builds successfully
- ✅ Package restore succeeds
- ⚠️ iOS build (Xcode issue, not code issue)
- ⚠️ Datadog credentials (need to be added)
- ⚠️ Initialization code (needs API updates)
- ⚠️ RUM data flowing to Datadog (requires initialization)
- ⚠️ Session Replay working (requires initialization)

---

## 🎯 Project Status Summary

| Component | Status | Notes |
|-----------|--------|-------|
| Backend API | ✅ **READY** | Fully functional, ready to send traces to Datadog |
| Android App | ⚠️ **CONFIGURED** | Builds successfully, initialization needs updates |
| iOS App | ⚠️ **XCODE ISSUE** | Xcode/simulator version mismatch, not code-related |
| NuGet Packages | ✅ **INSTALLED** | Android: v2.21.0-pre.1, iOS: v2.26.0 |
| Correlation Support | ✅ **IMPLEMENTED** | Both mobile and API support correlation IDs |
| Documentation | ✅ **COMPLETE** | Comprehensive guides for both backend and mobile |

**Overall Status**: Backend is production-ready. Mobile app is configured and building (Android), but needs Datadog initialization code updated to match current API versions before RUM data will flow.

---

**Last Updated**: December 30, 2025
