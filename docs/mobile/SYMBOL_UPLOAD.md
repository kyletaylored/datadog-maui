# Datadog Symbol Upload for MAUI

This guide explains how to configure and use automatic symbol upload for Android (ProGuard/R8) and iOS (dSYM) to Datadog for crash symbolication and error tracking.

## Overview

The MAUI app uses the **Datadog.MAUI.Symbols** NuGet package (v1.0.0) from the local-packages directory to automatically upload:

- **Android**: ProGuard/R8 mapping files for deobfuscating stack traces
- **iOS**: dSYM files for crash symbolication

This enables Datadog Error Tracking to show readable stack traces for production crashes.

## Configuration

### Project Setup

The [MauiApp/DatadogMauiApp.csproj](../../MauiApp/DatadogMauiApp.csproj) includes:

1. **Local Package Source** (lines 152-154):
```xml
<!-- Configure local package source for Datadog.MAUI.Symbols -->
<ItemGroup>
  <RestoreAdditionalProjectSources Include="$(MSBuildProjectDirectory)/../local-packages" />
</ItemGroup>
```

2. **Package Reference** (line 149):
```xml
<!-- Datadog Symbol Upload Package (from local-packages) -->
<PackageReference Include="Datadog.MAUI.Symbols" Version="1.0.0" />
```

3. **Symbol Upload Properties** (lines 68-82):
```xml
<!-- Datadog Symbol Upload Configuration -->
<PropertyGroup>
  <!-- Required: Service names for symbol uploads -->
  <DatadogServiceNameAndroid>com.datadog.mauiapp.android</DatadogServiceNameAndroid>
  <DatadogServiceNameiOS>com.datadog.mauiapp.ios</DatadogServiceNameiOS>

  <!-- API Key via environment variable (recommended) -->
  <!-- Set DD_API_KEY environment variable before building -->

  <!-- Optional: Test with dry-run first (set to true to skip actual upload) -->
  <DatadogDryRun>false</DatadogDryRun>
</PropertyGroup>

<!-- Android R8 Configuration (for ProGuard/R8 mapping file upload) -->
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND '$(TargetFramework)' == 'net10.0-android'">
  <!-- Enable R8 code shrinker (generates mapping files for Datadog) -->
  <AndroidEnableProguard>true</AndroidEnableProguard>
  <AndroidLinkTool>r8</AndroidLinkTool>
  <AndroidLinkMode>Full</AndroidLinkMode>
</PropertyGroup>
```

### Required Properties

| Property | Description | Example |
|----------|-------------|---------|
| `DatadogServiceNameAndroid` | Service name for Android symbol uploads | `com.datadog.mauiapp.android` |
| `DatadogServiceNameiOS` | Service name for iOS symbol uploads | `com.datadog.mauiapp.ios` |
| `DD_API_KEY` | Datadog API key (environment variable) | Set via `export DD_API_KEY=your_key` |

### Optional Properties

| Property | Description | Default |
|----------|-------------|---------|
| `DatadogDryRun` | Test mode - skip actual upload | `false` |

## How It Works

### Android Symbol Upload (R8 Mapping Files)

1. **Release Build Generates Mapping File**:
   - When building in Release configuration, R8 code shrinker is enabled
   - R8 generates `mapping.txt` in the build output
   - This file maps obfuscated class/method names back to original names

2. **Datadog.MAUI.Symbols Uploads Automatically**:
   - After the build completes, the package's MSBuild targets trigger
   - Reads `DD_API_KEY` from environment
   - Uploads `mapping.txt` to Datadog with metadata:
     - Service: `DatadogServiceNameAndroid`
     - Version: `ApplicationVersion` from csproj
     - Build ID: Generated from build output

3. **Datadog Uses Mapping for Stack Traces**:
   - When an error occurs in production, Datadog receives obfuscated stack trace
   - Datadog looks up the mapping file by service + version + build ID
   - Deobfuscates the stack trace automatically

### iOS Symbol Upload (dSYM Files)

1. **Release Build Generates dSYM**:
   - When building in Release configuration with AOT enabled (lines 46-54)
   - Xcode/msbuild generates `.dSYM` bundle
   - Contains debug symbols for crash symbolication

2. **Datadog.MAUI.Symbols Uploads Automatically**:
   - After the build completes, the package's MSBuild targets trigger
   - Reads `DD_API_KEY` from environment
   - Uploads dSYM bundle to Datadog with metadata:
     - Service: `DatadogServiceNameiOS`
     - Version: `ApplicationVersion` from csproj
     - Build ID: Extracted from dSYM UUID

3. **Datadog Uses dSYM for Crash Reports**:
   - When a crash occurs in production, Datadog receives raw memory addresses
   - Datadog looks up the dSYM by bundle ID and UUID
   - Symbolizes the crash report to show file names and line numbers

## Usage

### Building with Symbol Upload

#### Android Release Build

```bash
# Set API key
export DD_API_KEY="your_datadog_api_key_here"

# Build Android Release
dotnet build MauiApp/DatadogMauiApp.csproj -f net10.0-android -c Release

# Expected output:
# [MSBuild] 🐕 Generating Android RUM config file...
# DatadogMauiApp -> /path/to/bin/Release/net10.0-android/DatadogMauiApp.dll
# [Datadog] Uploading Android R8 mapping file...
# [Datadog] ✅ Successfully uploaded symbols for com.datadog.mauiapp.android
```

#### iOS Release Build

```bash
# Set API key
export DD_API_KEY="your_datadog_api_key_here"

# Build iOS Release (requires macOS with Xcode)
dotnet build MauiApp/DatadogMauiApp.csproj -f net10.0-ios -c Release

# Expected output:
# [MSBuild] 🐕 Generating iOS RUM config file...
# DatadogMauiApp -> /path/to/bin/Release/net10.0-ios/DatadogMauiApp.app
# [Datadog] Uploading iOS dSYM...
# [Datadog] ✅ Successfully uploaded symbols for com.datadog.mauiapp.ios
```

### Testing with Dry Run

To test the configuration without actually uploading:

```bash
# Enable dry run in csproj
<DatadogDryRun>true</DatadogDryRun>

# Build will simulate upload but not send to Datadog
dotnet build -f net10.0-android -c Release
```

### CI/CD Integration

#### GitHub Actions Example

```yaml
name: Build and Upload Symbols

on:
  push:
    branches: [main]

jobs:
  build-android:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'

      - name: Build Android Release
        env:
          DD_API_KEY: ${{ secrets.DD_API_KEY }}
          DD_RUM_ANDROID_CLIENT_TOKEN: ${{ secrets.DD_RUM_ANDROID_CLIENT_TOKEN }}
          DD_RUM_ANDROID_APPLICATION_ID: ${{ secrets.DD_RUM_ANDROID_APPLICATION_ID }}
        run: |
          dotnet build MauiApp/DatadogMauiApp.csproj \
            -f net10.0-android \
            -c Release

  build-ios:
    runs-on: macos-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'

      - name: Build iOS Release
        env:
          DD_API_KEY: ${{ secrets.DD_API_KEY }}
          DD_RUM_IOS_CLIENT_TOKEN: ${{ secrets.DD_RUM_IOS_CLIENT_TOKEN }}
          DD_RUM_IOS_APPLICATION_ID: ${{ secrets.DD_RUM_IOS_APPLICATION_ID }}
        run: |
          dotnet build MauiApp/DatadogMauiApp.csproj \
            -f net10.0-ios \
            -c Release
```

## Verification

### Verify Android Mapping File Upload

1. **Check build output** for upload confirmation:
   ```
   [Datadog] Uploading Android R8 mapping file...
   [Datadog] ✅ Successfully uploaded symbols
   ```

2. **Check Datadog UI**:
   - Go to: https://app.datadoghq.com/error-tracking
   - Navigate to: APM → Error Tracking → Settings → Symbol Files
   - Filter by: `service:com.datadog.mauiapp.android`
   - You should see uploaded mapping files with version numbers

3. **Test with an error**:
   ```csharp
   // In your MAUI app, trigger an error
   throw new Exception("Test exception for symbol upload verification");
   ```

   - Check Error Tracking in Datadog
   - Stack trace should show original class/method names (not obfuscated)

### Verify iOS dSYM Upload

1. **Check build output** for upload confirmation:
   ```
   [Datadog] Uploading iOS dSYM...
   [Datadog] ✅ Successfully uploaded symbols
   ```

2. **Check Datadog UI**:
   - Go to: https://app.datadoghq.com/error-tracking
   - Navigate to: APM → Error Tracking → Settings → Symbol Files
   - Filter by: `service:com.datadog.mauiapp.ios`
   - You should see uploaded dSYM bundles with UUIDs

3. **Test with a crash**:
   - Trigger a crash in the iOS app
   - Check Error Tracking in Datadog
   - Crash report should show file names and line numbers (symbolicated)

## Troubleshooting

### Issue 1: "DD_API_KEY not found"

**Symptom:** Build completes but no upload happens, or upload fails with authentication error.

**Solution:**
```bash
# Verify DD_API_KEY is set
echo $DD_API_KEY

# If not set, export it
export DD_API_KEY="your_api_key_here"

# Rebuild
dotnet clean
dotnet build -f net10.0-android -c Release
```

### Issue 2: "Mapping file not found" (Android)

**Symptom:** Build succeeds but Datadog.MAUI.Symbols can't find mapping.txt.

**Root Cause:** R8 not enabled or Debug configuration used.

**Solution:**
1. **Verify Release configuration**:
   ```bash
   dotnet build -f net10.0-android -c Release
   ```

2. **Check csproj has R8 config** (lines 84-91):
   ```xml
   <PropertyGroup Condition="'$(Configuration)' == 'Release' AND '$(TargetFramework)' == 'net10.0-android'">
     <AndroidEnableProguard>true</AndroidEnableProguard>
     <AndroidLinkTool>r8</AndroidLinkTool>
     <AndroidLinkMode>Full</AndroidLinkMode>
   </PropertyGroup>
   ```

3. **Verify mapping file exists**:
   ```bash
   find bin/Release/net10.0-android -name "mapping.txt"
   ```

### Issue 3: "dSYM not found" (iOS)

**Symptom:** Build succeeds but Datadog.MAUI.Symbols can't find dSYM bundle.

**Root Cause:** dSYM generation not enabled or Debug configuration used.

**Solution:**
1. **Verify Release configuration**:
   ```bash
   dotnet build -f net10.0-ios -c Release
   ```

2. **Check csproj has dSYM config** (lines 46-54):
   ```xml
   <PropertyGroup Condition="'$(Configuration)' == 'Release' AND '$(TargetFramework)' == 'net10.0-ios'">
     <MtouchDebug>false</MtouchDebug>
     <MtouchSymbolsList>true</MtouchSymbolsList>
     <MtouchLink>Full</MtouchLink>
     <UseInterpreter>false</UseInterpreter>
   </PropertyGroup>
   ```

3. **Verify dSYM exists**:
   ```bash
   find bin/Release/net10.0-ios -name "*.dSYM"
   ```

### Issue 4: "Upload fails with 403 Forbidden"

**Symptom:** Build completes, symbols found, but upload fails with authentication error.

**Solution:**
1. **Verify API key has correct permissions**:
   - Go to: https://app.datadoghq.com/organization-settings/api-keys
   - API key must have "Source Code Integration" permission

2. **Check API key is not a Client Token**:
   - `DD_API_KEY` must be an API Key (starts with something like `abc123...`)
   - NOT a Client Token (starts with `pub...`)

### Issue 5: ProGuard vs R8 Error (Android)

**Symptom:** Build fails with:
```
error XA1011: Using ProGuard with the D8 DEX compiler is no longer supported.
Please set the code shrinker to 'r8'
```

**Solution:**
This is already fixed in the csproj (lines 88-89):
```xml
<AndroidEnableProguard>true</AndroidEnableProguard>
<AndroidLinkTool>r8</AndroidLinkTool>
```

If you still see this error, ensure you're using the latest csproj configuration.

## Advanced Configuration

### Custom Service Names

You can override service names via MSBuild properties:

```bash
dotnet build -f net10.0-android -c Release \
  -p:DatadogServiceNameAndroid="com.mycompany.myapp.android"
```

### Multiple Environments

Use different service names per environment:

```xml
<!-- Development -->
<DatadogServiceNameAndroid Condition="'$(Configuration)' == 'Debug'">
  com.datadog.mauiapp.android.dev
</DatadogServiceNameAndroid>

<!-- Production -->
<DatadogServiceNameAndroid Condition="'$(Configuration)' == 'Release'">
  com.datadog.mauiapp.android
</DatadogServiceNameAndroid>
```

### Version Tracking

The package automatically uses `ApplicationVersion` from csproj:

```xml
<ApplicationVersion>1</ApplicationVersion>
```

Increment this for each release to track symbols per version.

## Additional Resources

- [Datadog Error Tracking Docs](https://docs.datadoghq.com/real_user_monitoring/error_tracking/mobile/android/)
- [Android R8 Documentation](https://developer.android.com/studio/build/shrink-code)
- [iOS dSYM Documentation](https://developer.apple.com/documentation/xcode/building-your-app-to-include-debugging-information)
- [iOS Crash Reporting Guide](ios/CRASH_REPORTING.md)

---

**Status:** ✅ Configured

**Platforms:**
- ✅ Android (R8 mapping files)
- ✅ iOS (dSYM files)

**Last Updated:** 2026-01-28
