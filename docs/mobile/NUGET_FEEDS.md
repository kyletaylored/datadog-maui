# NuGet Package Sources for Datadog MAUI

This guide explains how to use custom NuGet feeds for Datadog MAUI SDK packages.

## Overview

The project uses three NuGet package sources:

1. **nuget.org** - Official NuGet.org repository (default, highest priority)
2. **DatadogMAUI** - Static GitHub Pages feed at `https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json`
3. **Local** - Local packages directory at `./local-packages`

## Configuration

### NuGet.config (Repository Root)

The [NuGet.config](/NuGet.config) file at the repository root configures package sources for all projects:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <!-- Official NuGet.org source (takes priority for MAUI app) -->
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />

    <!-- Local packages directory (available but lower priority) -->
    <add key="Local" value="./local-packages" />

    <!-- Datadog MAUI SDK - Static NuGet feed (GitHub Pages) -->
    <add key="DatadogMAUI" value="https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json" />
  </packageSources>
</configuration>
```

### Project Configuration (DatadogMauiApp.csproj)

The [MauiApp/DatadogMauiApp.csproj](../../MauiApp/DatadogMauiApp.csproj) also specifies additional sources using `RestoreAdditionalProjectSources`:

```xml
<!-- Configure package sources for Datadog packages -->
<ItemGroup>
  <!-- Local package source -->
  <RestoreAdditionalProjectSources Include="$(MSBuildProjectDirectory)/../local-packages" />

  <!-- Remote static NuGet feed for Datadog MAUI packages -->
  <RestoreAdditionalProjectSources Include="https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json" />
</ItemGroup>
```

**Priority Order:**
1. NuGet.config sources (nuget.org → Local → DatadogMAUI)
2. RestoreAdditionalProjectSources (in order listed)

## Available Packages

### Main Package

| Package ID | Version | Description |
|------------|---------|-------------|
| **Datadog.MAUI** | 3.5.0 | Main Datadog MAUI SDK package |

### Android Packages

| Package ID | Version | Description |
|------------|---------|-------------|
| Datadog.MAUI.Android.Binding | 3.5.0 | Android platform bindings |
| Datadog.MAUI.Android.Core | 3.5.0 | Core Android SDK |
| Datadog.MAUI.Android.Flags | 3.5.0 | Feature flags for Android |
| Datadog.MAUI.Android.Internal | 3.5.0 | Internal Android utilities |
| Datadog.MAUI.Android.Logs | 3.5.0 | Android logging |
| Datadog.MAUI.Android.NDK | 3.5.0 | NDK crash reporting |
| Datadog.MAUI.Android.OkHttp | 3.5.0 | OkHttp integration |
| Datadog.MAUI.Android.OkHttp.OpenTelemetry | 3.5.0 | OpenTelemetry for OkHttp |
| Datadog.MAUI.Android.OpenTracingApi | 3.5.0 | OpenTracing API |
| Datadog.MAUI.Android.RUM | 3.5.0 | Android RUM (Real User Monitoring) |
| Datadog.MAUI.Android.SessionReplay | 3.5.0 | Session Replay for Android |
| Datadog.MAUI.Android.Trace | 3.5.0 | APM Tracing for Android |
| Datadog.MAUI.Android.Trace.OpenTelemetry | 3.5.0 | OpenTelemetry tracing |
| Datadog.MAUI.Android.WebView | 3.5.0 | WebView tracking |

### iOS Packages

| Package ID | Version | Description |
|------------|---------|-------------|
| Datadog.MAUI.iOS.Binding | 3.5.0 | iOS platform bindings |
| Datadog.MAUI.iOS.Core | 3.5.0 | Core iOS SDK |
| Datadog.MAUI.iOS.CrashReporting | 3.5.0 | Crash reporting for iOS |
| Datadog.MAUI.iOS.Flags | 3.5.0 | Feature flags for iOS |
| Datadog.MAUI.iOS.Internal | 3.5.0 | Internal iOS utilities |
| Datadog.MAUI.iOS.Logs | 3.5.0 | iOS logging |
| Datadog.MAUI.iOS.OpenTelemetryApi | 3.5.0 | OpenTelemetry API |
| Datadog.MAUI.iOS.RUM | 3.5.0 | iOS RUM (Real User Monitoring) |
| Datadog.MAUI.iOS.SessionReplay | 3.5.0 | Session Replay for iOS |
| Datadog.MAUI.iOS.Trace | 3.5.0 | APM Tracing for iOS |
| Datadog.MAUI.iOS.WebViewTracking | 3.5.0 | WebView tracking |

### Symbol Upload

| Package ID | Version | Description |
|------------|---------|-------------|
| Datadog.MAUI.Symbols | 1.0.0 | Symbol upload for crash symbolication |

## Usage

### Listing Available Packages

Search for Datadog packages in your feed:

```bash
dotnet package search "Datadog" \
  --source "https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json" \
  --prerelease
```

### Installing Packages

#### Option 1: Add to csproj (Recommended)

Edit [MauiApp/DatadogMauiApp.csproj](../../MauiApp/DatadogMauiApp.csproj):

```xml
<ItemGroup>
  <PackageReference Include="Datadog.MAUI" Version="3.5.0" />
</ItemGroup>
```

Then restore:
```bash
dotnet restore
```

#### Option 2: Using dotnet CLI

```bash
cd MauiApp
dotnet add package Datadog.MAUI --version 3.5.0 --source DatadogMAUI
```

### Platform-Specific Packages

For Android-only or iOS-only packages:

```xml
<!-- Android-specific -->
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-android'">
  <PackageReference Include="Datadog.MAUI.Android.SessionReplay" Version="3.5.0" />
</ItemGroup>

<!-- iOS-specific -->
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-ios'">
  <PackageReference Include="Datadog.MAUI.iOS.CrashReporting" Version="3.5.0" />
</ItemGroup>
```

## Managing Package Sources

### List Configured Sources

```bash
dotnet nuget list source
```

Expected output:
```
Registered Sources:
  1.  nuget.org [Enabled]
      https://api.nuget.org/v3/index.json
  2.  Local [Enabled]
      /path/to/local-packages
  3.  DatadogMAUI [Enabled]
      https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json
```

### Add Source (Manual)

If the source isn't already configured:

```bash
dotnet nuget add source "https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json" \
  --name "DatadogMAUI"
```

### Remove Source

```bash
dotnet nuget remove source "DatadogMAUI"
```

### Update Source

```bash
dotnet nuget update source "DatadogMAUI" \
  --source "https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json"
```

## Troubleshooting

### Issue 1: Package Not Found

**Symptom:** `error NU1101: Unable to find package Datadog.MAUI`

**Solutions:**

1. **Verify source is configured:**
   ```bash
   dotnet nuget list source
   ```

2. **Check if package exists:**
   ```bash
   dotnet package search "Datadog.MAUI" --source DatadogMAUI
   ```

3. **Clear NuGet cache:**
   ```bash
   dotnet nuget locals all --clear
   dotnet restore --no-cache
   ```

4. **Verify NuGet.config syntax:**
   - Check for typos in feed URL
   - Ensure XML is well-formed

### Issue 2: Restore Using Wrong Source

**Symptom:** Package restores from nuget.org instead of DatadogMAUI feed.

**Root Cause:** Source priority order.

**Solution:**

The `<clear />` in NuGet.config removes inherited sources, ensuring only our sources are used:

```xml
<packageSources>
  <clear />  <!-- Important: Clear inherited sources first -->
  <add key="nuget.org" value="..." />
  <add key="DatadogMAUI" value="..." />
</packageSources>
```

### Issue 3: Feed Not Accessible

**Symptom:** `error NU1301: Unable to load the service index for source`

**Solutions:**

1. **Check internet connection**

2. **Verify feed URL is accessible:**
   ```bash
   curl -I "https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json"
   ```

3. **Check for HTTPS certificate issues:**
   ```bash
   curl -v "https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json"
   ```

4. **Temporarily test with HTTP (not recommended for production):**
   ```xml
   <add key="DatadogMAUI"
        value="http://kyletaylored.github.io/dd-sdk-maui/nuget/index.json" />
   ```

### Issue 4: Version Conflicts

**Symptom:** `error NU1107: Version conflict detected for Datadog.MAUI`

**Solution:**

1. **Check all package references:**
   ```bash
   dotnet list package --include-transitive | grep Datadog
   ```

2. **Pin to specific version in all projects:**
   ```xml
   <PackageReference Include="Datadog.MAUI" Version="3.5.0" />
   ```

3. **Use Central Package Management (Directory.Packages.props):**
   ```xml
   <Project>
     <PropertyGroup>
       <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
     </PropertyGroup>
     <ItemGroup>
       <PackageVersion Include="Datadog.MAUI" Version="3.5.0" />
     </ItemGroup>
   </Project>
   ```

## Feed Architecture

### Static Feed Structure (GitHub Pages)

The feed at `https://kyletaylored.github.io/dd-sdk-maui/nuget/` uses the NuGet v3 protocol with a static file structure:

```
nuget/
├── index.json                          # Service index (entry point)
└── flatcontainer/                      # Package storage
    ├── datadog.maui/
    │   ├── index.json                  # Version list
    │   ├── 3.5.0/
    │   │   └── datadog.maui.3.5.0.nupkg
    ├── datadog.maui.android.core/
    │   ├── index.json
    │   └── 3.5.0/
    │       └── datadog.maui.android.core.3.5.0.nupkg
    └── ...
```

**Key Features:**
- ✅ No server-side processing required
- ✅ Hosted on GitHub Pages (free, reliable)
- ✅ Works with standard NuGet clients
- ✅ Supports versioning and package updates
- ✅ Can be mirrored/cached by NuGet

### Service Index (index.json)

The service index defines available resources:

```json
{
  "@context": {
    "@vocab": "http://schema.nuget.org/services#"
  },
  "version": "3.0.0",
  "resources": [
    {
      "@id": "https://kyletaylored.github.io/dd-sdk-maui/nuget/flatcontainer/",
      "@type": "PackageBaseAddress/3.0.0"
    },
    {
      "@id": "https://kyletaylored.github.io/dd-sdk-maui/nuget/",
      "@type": "PackagePublish/2.0.0"
    }
  ]
}
```

## Comparison: Remote vs Local Packages

| Feature | Remote Feed (GitHub Pages) | Local Packages |
|---------|---------------------------|----------------|
| **Access** | Internet required | Always available |
| **Version Control** | Managed externally | Manual management |
| **Sharing** | Easy (URL) | Requires file distribution |
| **CI/CD** | Works out-of-box | Requires committing packages |
| **Size** | No repo bloat | Increases repo size |
| **Updates** | Automatic via feed | Manual file replacement |
| **Offline** | ❌ No | ✅ Yes |

**Best Practice:** Use remote feed for CI/CD and team collaboration, keep local packages for testing/development.

## Migration from Bcr.* Packages

If migrating from older `Bcr.Datadog.*` packages:

### Before (Bcr packages)
```xml
<PackageReference Include="Bcr.Datadog.Android.Sdk.Core" Version="2.21.0-pre.1" />
<PackageReference Include="Bcr.Datadog.Android.Sdk.Logs" Version="2.21.0-pre.1" />
<PackageReference Include="Bcr.Datadog.Android.Sdk.Rum" Version="2.21.0-pre.1" />
```

### After (Datadog.MAUI packages)
```xml
<PackageReference Include="Datadog.MAUI" Version="3.5.0" />
<!-- Or platform-specific: -->
<PackageReference Include="Datadog.MAUI.Android.Core" Version="3.5.0" />
<PackageReference Include="Datadog.MAUI.Android.Logs" Version="3.5.0" />
<PackageReference Include="Datadog.MAUI.Android.RUM" Version="3.5.0" />
```

**Breaking Changes:**
- Namespace changes: `Bcr.Datadog.*` → `Datadog.MAUI.*`
- API surface may differ (check migration guide)
- Configuration patterns may have changed

## Additional Resources

- [NuGet Package Sources Documentation](https://learn.microsoft.com/en-us/nuget/consume-packages/configuring-nuget-behavior)
- [NuGet v3 Protocol](https://learn.microsoft.com/en-us/nuget/api/overview)
- [Static NuGet Feeds](https://learn.microsoft.com/en-us/nuget/hosting-packages/overview)
- [Symbol Upload Guide](SYMBOL_UPLOAD.md)

---

**Status:** ✅ Configured

**Feed URL:** `https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json`

**Last Updated:** 2026-01-28

**Total Packages:** 27
