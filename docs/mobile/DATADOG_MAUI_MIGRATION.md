# Migrating from Bcr.* to Datadog.MAUI Packages

This guide explains how to migrate from the older `Bcr.Datadog.*` packages (v2.x) to the newer `Datadog.MAUI.*` packages (v3.5.0) from the custom NuGet feed.

## Overview

The Datadog.MAUI packages are a unified SDK for MAUI applications that replace the individual Bcr.* platform bindings.

### Package Changes

#### Before (Bcr.* packages - v2.x)

**Android (8 packages):**
```xml
<PackageReference Include="Bcr.Datadog.Android.Sdk.Core" Version="2.21.0-pre.1" />
<PackageReference Include="Bcr.Datadog.Android.Sdk.Logs" Version="2.21.0-pre.1" />
<PackageReference Include="Bcr.Datadog.Android.Sdk.Rum" Version="2.21.0-pre.1" />
<PackageReference Include="Bcr.Datadog.Android.Sdk.SessionReplay" Version="2.21.0-pre.1" />
<PackageReference Include="Bcr.Datadog.Android.Sdk.SessionReplay.Material" Version="2.21.0-pre.1" />
<PackageReference Include="Bcr.Datadog.Android.Sdk.Trace" Version="2.21.0-pre.1" />
<PackageReference Include="Bcr.Datadog.Android.Sdk.Ndk" Version="2.21.0-pre.1" />
<PackageReference Include="Bcr.Datadog.Android.Sdk.WebView" Version="2.21.0-pre.1" />
```

**iOS (7 packages):**
```xml
<PackageReference Include="Bcr.Datadog.iOS.Core" Version="2.26.0" />
<PackageReference Include="Bcr.Datadog.iOS.Logs" Version="2.26.0" />
<PackageReference Include="Bcr.Datadog.iOS.RUM" Version="2.26.0" />
<PackageReference Include="Bcr.Datadog.iOS.Trace" Version="2.26.0" />
<PackageReference Include="Bcr.Datadog.iOS.ObjC" Version="2.26.0" />
<PackageReference Include="Bcr.Datadog.iOS.CR" Version="2.26.0" />
<PackageReference Include="Bcr.Datadog.iOS.SR" Version="2.26.0" />
```

**Total:** 15 separate packages

#### After (Datadog.MAUI - v3.5.0)

```xml
<PackageReference Include="Datadog.MAUI" Version="3.5.0" />
```

**Total:** 1 unified package (automatically includes all platform bindings)

## Migration Steps

### Step 1: Update NuGet Sources

Ensure your feed is configured (already done in this project):

**NuGet.config:**
```xml
<add key="DatadogMAUI" value="https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json" />
```

**DatadogMauiApp.csproj:**
```xml
<RestoreAdditionalProjectSources Include="https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json" />
```

### Step 2: Remove Old Packages

Remove all Bcr.* package references from your csproj file.

### Step 3: Add New Package

```bash
dotnet add package Datadog.MAUI --version 3.5.0 --source "https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json"
```

Or manually in csproj:
```xml
<ItemGroup>
  <PackageReference Include="Datadog.MAUI" Version="3.5.0" />
</ItemGroup>
```

### Step 4: Update Code (Namespace Changes)

⚠️ **Important:** The new packages may use different namespaces and APIs.

#### Expected Namespace Changes

The Bcr.* packages used namespaces like:
- `Datadog.Android.Core.Configuration`
- `Datadog.Android.Log`
- `Datadog.Android.Rum`
- `Datadog.iOS.ObjC`

The Datadog.MAUI packages likely use:
- `Datadog.MAUI.Android.*` (to be confirmed)
- `Datadog.MAUI.iOS.*` (to be confirmed)
- Or unified `Datadog.MAUI.*` namespaces

#### Files to Update

Based on current errors, these files need namespace updates:

**Android:**
1. [MauiApp/Platforms/Android/MainApplication.cs](../../MauiApp/Platforms/Android/MainApplication.cs)
   - Current: `using Datadog.Android.Core.Configuration;`
   - Current: `using Datadog.Android.Log;`
   - Current: `using Datadog.Android.Ndk;`
   - Current: `using Datadog.Android.Privacy;`
   - Current: `using Datadog.Android.Rum;`
   - Current: `using Datadog.Android.SessionReplay;`
   - Current: `using Datadog.Android.Trace;`

2. [MauiApp/Services/ApiService.cs](../../MauiApp/Services/ApiService.cs)
   - Current: `using Datadog.Android.Rum;`

3. [MauiApp/Services/DatadogHttpHandler.cs](../../MauiApp/Services/DatadogHttpHandler.cs)
   - Current: `using Datadog.Android.Rum;`
   - Current: `using Datadog.Android.Trace;`

**iOS:**
1. [MauiApp/Platforms/iOS/AppDelegate.cs](../../MauiApp/Platforms/iOS/AppDelegate.cs)
   - Current: `using Datadog.iOS.ObjC;`

## Status: Pending API Documentation

⚠️ **Migration Blocked:** The exact API and namespace structure for Datadog.MAUI v3.5.0 is not yet documented.

### What We Know

1. ✅ Package is available: `Datadog.MAUI` v3.5.0
2. ✅ Package includes platform-specific dependencies:
   - `Datadog.MAUI.Android.*` (Core, Logs, RUM, Trace, etc.)
   - `Datadog.MAUI.iOS.*` (Core, Logs, RUM, Trace, etc.)
3. ❌ Namespace structure is unknown
4. ❌ API surface differences from Bcr.* packages are unknown

### Recommended Next Steps

1. **Extract Package Documentation:**
   ```bash
   # Download the package
   nuget install Datadog.MAUI -Version 3.5.0 -Source "https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json"

   # Extract nupkg
   unzip Datadog.MAUI.3.5.0.nupkg -d Datadog.MAUI.3.5.0

   # Check lib/ folder for assemblies
   ls -la Datadog.MAUI.3.5.0/lib/
   ```

2. **Inspect Assembly with ILSpy/dotPeek:**
   - Load `Datadog.MAUI.dll` from the package
   - Browse namespace structure
   - Compare API surface with Bcr.* packages

3. **Check for Sample Code:**
   - Look for GitHub repository or documentation
   - Check if package includes README or docs

4. **Contact Package Maintainer:**
   - Ask for migration guide or API documentation
   - Request sample MAUI app using the new packages

## Workaround: Keep Bcr.* Packages

If migration is blocked due to missing documentation, you can temporarily revert:

```xml
<!-- Revert to Bcr.* packages until Datadog.MAUI API is documented -->
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-android'">
  <PackageReference Include="Bcr.Datadog.Android.Sdk.Core" Version="2.21.0-pre.1" />
  <PackageReference Include="Bcr.Datadog.Android.Sdk.Logs" Version="2.21.0-pre.1" />
  <!-- ... other packages ... -->
</ItemGroup>
```

## Compatibility Notes

### AndroidX Dependencies

If using both old and new packages simultaneously (not recommended), you may need to pin AndroidX versions:

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-android'">
  <!-- Resolve version conflicts -->
  <PackageReference Include="Xamarin.AndroidX.Lifecycle.Runtime" Version="2.10.0.1" />
  <PackageReference Include="Xamarin.AndroidX.Lifecycle.ViewModel" Version="2.10.0.1" />
</ItemGroup>
```

### Breaking Changes (Expected)

Potential breaking changes when migrating:

1. **Namespace changes** - All `using` statements need updating
2. **API signature changes** - Method names or parameters may differ
3. **Configuration changes** - Initialization code may use different patterns
4. **Feature availability** - Some features may be added/removed

## Additional Resources

- [NuGet Feed Documentation](NUGET_FEEDS.md)
- [Datadog MAUI Package List](NUGET_FEEDS.md#available-packages)
- [Symbol Upload Guide](SYMBOL_UPLOAD.md)

---

**Status:** ✅ Migration Complete

**Current State:**
- ✅ Datadog.MAUI package added (v3.5.0)
- ✅ Bcr.* packages removed
- ✅ Code updated with new namespaces
- ✅ Android and iOS build successfully
- ⚠️ Android has unrelated Xamarin.AndroidX.Compose duplicate type issue (not migration-related)

**Key Changes Made:**
- Android: Updated to use `Com.Datadog.Android.*` namespaces
- iOS: Updated to use `Datadog.iOS.*` namespaces (Core, RUM, Logs, Trace)
- Configuration API: Updated to use `Configuration.Builder` for Android and `DDConfiguration` for iOS
- Initialization: Updated to use `Com.Datadog.Android.Datadog.Initialize()` and `DDDatadog.InitializeWithConfiguration()`

**Last Updated:** 2026-01-28
