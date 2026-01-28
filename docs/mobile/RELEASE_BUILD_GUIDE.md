# Release Build and Symbol Upload Guide

This guide explains how to build Release versions of the MAUI app and test the Datadog symbol upload functionality.

## Quick Start

### Test Symbol Upload (Dry-Run Mode)

Safe testing without actual upload to Datadog:

```bash
# Android Release (dry-run)
make app-release-android-dry

# iOS Release (dry-run)
make app-release-ios-dry
```

### Real Symbol Upload

Upload symbols to Datadog (requires API key):

```bash
# Set your Datadog API key
export DD_API_KEY="your-datadog-api-key"

# Android Release (with upload)
make app-release-android

# iOS Release (with upload)
make app-release-ios
```

## Available Make Commands

| Command | Description |
|---------|-------------|
| `make app-release-android-dry` | Build Android Release in dry-run mode (simulates upload) |
| `make app-release-ios-dry` | Build iOS Release in dry-run mode (simulates upload) |
| `make app-release-android` | Build Android Release with actual symbol upload |
| `make app-release-ios` | Build iOS Release with actual symbol upload |

## Configuration

### Dry-Run Mode (Default)

The project is configured in dry-run mode by default in [MauiApp/DatadogMauiApp.csproj](../../MauiApp/DatadogMauiApp.csproj#L78):

```xml
<DatadogDryRun>true</DatadogDryRun>
```

In dry-run mode:
- ✅ Release builds complete successfully
- ✅ Symbol files are generated (R8 mappings, dSYMs)
- ✅ Upload process is simulated
- ✅ Logs show what would be uploaded
- ❌ Nothing is actually sent to Datadog

### Enable Real Upload

To enable actual symbol upload:

1. **Edit the csproj file**:
   ```xml
   <DatadogDryRun>false</DatadogDryRun>
   ```

2. **Set the API key**:
   ```bash
   export DD_API_KEY="your-datadog-api-key"
   ```

3. **Build Release**:
   ```bash
   make app-release-android  # or app-release-ios
   ```

## What Gets Uploaded

### Android (R8 Mapping File)

**File Generated:**
- `obj/Release/net10.0-android/android/assets/mapping.txt`

**Purpose:**
- Deobfuscates crash stack traces
- Maps minified code back to original class/method names
- Required when using R8/ProGuard code shrinking

**Upload Details:**
- Service Name: `com.datadog.mauiapp.android`
- API Endpoint: Datadog Symbol Upload API
- Format: ProGuard/R8 mapping file

### iOS (dSYM Bundle)

**File Generated:**
- `bin/Release/net10.0-ios/iossimulator-arm64/*.app.dSYM/`

**Purpose:**
- Symbolicates crash reports
- Maps memory addresses back to source code locations
- Essential for iOS crash debugging

**Upload Details:**
- Service Name: `com.datadog.mauiapp.ios`
- API Endpoint: Datadog Symbol Upload API
- Format: dSYM bundle (ZIP compressed)

## Expected Output

### Dry-Run Mode Output

```bash
$ make app-release-android-dry

🔨 Building Android Release (dry-run mode)...
   Symbol upload will be simulated (no actual upload)

  [MSBuild] 🐕 Datadog Symbol Upload
  [MSBuild]   Service: com.datadog.mauiapp.android
  [MSBuild]   Dry Run: true
  [MSBuild]   API Key: [Set via DD_API_KEY]
  [MSBuild] DRY RUN - Would upload R8 mapping file
  [MSBuild] DRY RUN - File: obj/Release/net10.0-android/android/assets/mapping.txt
  [MSBuild] DRY RUN - Size: 15234 bytes
  [MSBuild] DRY RUN - Service: com.datadog.mauiapp.android

📋 Symbol Upload Summary:
   [MSBuild] DRY RUN - Would upload R8 mapping file

✅ Android Release build complete (dry-run)
   To test with actual upload, run: make app-release-android
```

### Real Upload Output

```bash
$ export DD_API_KEY="pub1234567890abcdef"
$ make app-release-android

🔨 Building Android Release with symbol upload...

✅ DD_API_KEY is set
⚠️  DatadogDryRun is currently: true (in csproj)
   To enable actual upload, edit MauiApp/DatadogMauiApp.csproj:
   Change <DatadogDryRun>true</DatadogDryRun> to false

  [MSBuild] 🐕 Datadog Symbol Upload
  [MSBuild]   Service: com.datadog.mauiapp.android
  [MSBuild]   Dry Run: false
  [MSBuild]   API Key: pub1234...cdef
  [MSBuild] 📤 Uploading R8 mapping file to Datadog...
  [MSBuild] ✅ Successfully uploaded R8 mapping file
  [MSBuild]    File: mapping.txt
  [MSBuild]    Size: 15234 bytes
  [MSBuild]    Service: com.datadog.mauiapp.android

✅ Android Release build complete
```

## Troubleshooting

### Error: DD_API_KEY not set

**Problem:**
```
❌ Error: DD_API_KEY environment variable not set
```

**Solution:**
```bash
export DD_API_KEY="your-datadog-api-key"
```

Get your API key from: https://app.datadoghq.com/organization-settings/api-keys

### Error: Mapping file not found

**Problem:**
```
❌ Error: R8 mapping file not found
```

**Solution:**
Ensure R8 is enabled in Release configuration. This is already configured in [DatadogMauiApp.csproj](../../MauiApp/DatadogMauiApp.csproj#L81-L87):

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND '$(TargetFramework)' == 'net10.0-android'">
  <AndroidEnableProguard>true</AndroidEnableProguard>
  <AndroidLinkTool>r8</AndroidLinkTool>
  <AndroidLinkMode>Full</AndroidLinkMode>
</PropertyGroup>
```

### Error: dSYM not found

**Problem:**
```
❌ Error: dSYM bundle not found
```

**Solution:**
Ensure dSYM generation is enabled for iOS Release builds. This is already configured in [DatadogMauiApp.csproj](../../MauiApp/DatadogMauiApp.csproj#L46-L54):

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND '$(TargetFramework)' == 'net10.0-ios'">
  <MtouchDebug>false</MtouchDebug>
  <MtouchSymbolsList>true</MtouchSymbolsList>
  <MtouchLink>Full</MtouchLink>
  <UseInterpreter>false</UseInterpreter>
</PropertyGroup>
```

### Android Compose Error

**Problem:**
```
Type androidx.compose.runtime.Immutable is defined multiple times
```

**Solution:**
This is a pre-existing issue with Xamarin.AndroidX.Compose packages (unrelated to Datadog or symbol upload). The iOS build works fine. To work around for Android, you can build without Compose dependencies or wait for package updates.

## Testing Symbol Upload

### 1. Test with Dry-Run First

Always test with dry-run mode first to verify the build works:

```bash
make app-release-android-dry
```

Check the output for:
- ✅ Build completes successfully
- ✅ "DRY RUN" messages appear
- ✅ Symbol files are found
- ✅ Service name is correct

### 2. Enable Real Upload

Once dry-run works:

1. Edit [DatadogMauiApp.csproj](../../MauiApp/DatadogMauiApp.csproj):
   ```xml
   <DatadogDryRun>false</DatadogDryRun>
   ```

2. Set API key:
   ```bash
   export DD_API_KEY="your-key"
   ```

3. Build:
   ```bash
   make app-release-android
   ```

### 3. Verify in Datadog

After uploading:

1. Go to **APM** → **Error Tracking** → **Source Code** in Datadog
2. Look for your service name:
   - `com.datadog.mauiapp.android`
   - `com.datadog.mauiapp.ios`
3. Verify symbol files are listed

### 4. Test Crash Symbolication

1. Build and deploy the Release app
2. Trigger a test crash (using the crash button in the Dashboard tab)
3. Check crash reports in Datadog
4. Verify stack traces are symbolicated (show actual method names, not memory addresses)

## Manual Build Commands

If you prefer not to use Make:

### Android Release (Manual)

```bash
# Dry-run (default)
cd MauiApp
dotnet build -c Release -f net10.0-android

# With actual upload
export DD_API_KEY="your-key"
# Edit csproj: <DatadogDryRun>false</DatadogDryRun>
dotnet build -c Release -f net10.0-android
```

### iOS Release (Manual)

```bash
# Dry-run (default)
cd MauiApp
dotnet build -c Release -f net10.0-ios

# With actual upload
export DD_API_KEY="your-key"
# Edit csproj: <DatadogDryRun>false</DatadogDryRun>
dotnet build -c Release -f net10.0-ios
```

## Build Artifacts

After a successful Release build, you'll find:

### Android
- **APK**: `bin/Release/net10.0-android/com.datadog.mauiapp-Signed.apk`
- **Mapping**: `obj/Release/net10.0-android/android/assets/mapping.txt`

### iOS
- **App**: `bin/Release/net10.0-ios/iossimulator-arm64/DatadogMauiApp.app`
- **dSYM**: `bin/Release/net10.0-ios/iossimulator-arm64/DatadogMauiApp.app.dSYM`

## Related Documentation

- [Symbol Upload Documentation](SYMBOL_UPLOAD.md) - Detailed symbol upload configuration
- [Diagnostics Page](DIAGNOSTICS_PAGE.md) - View current configuration
- [Datadog Configuration](RUM_CONFIGURATION.md) - RUM and APM setup

---

**Last Updated:** 2026-01-28

**Quick Commands:**
```bash
# Safe testing (no upload)
make app-release-android-dry
make app-release-ios-dry

# Real upload (requires DD_API_KEY)
export DD_API_KEY="your-key"
make app-release-android
make app-release-ios
```
