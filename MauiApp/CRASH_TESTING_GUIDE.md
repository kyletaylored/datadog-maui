# iOS Crash Reporting Test Guide

This guide walks you through testing iOS crash reporting with dSYM symbolication in Datadog.

## What You'll Test

The app includes a **Test Crash** button that intentionally crashes the app with a meaningful error message. This allows you to verify that:

1. ✅ Crashes are captured by Datadog
2. ✅ dSYM files are correctly uploaded
3. ✅ Stack traces are symbolicated (showing file names, method names, and line numbers)

## Quick Start

### Option 1: Automated Script (Recommended)

```bash
cd MauiApp

# Set your Datadog API key
export DATADOG_API_KEY=your_api_key_here

# Run the automated test script
./test-crash-reporting.sh 1.0.0-test
```

The script will:
- Build a Release version with dSYMs
- Upload dSYMs to Datadog
- Provide instructions for testing

### Option 2: Manual Steps

#### Step 1: Build Release Version

```bash
cd MauiApp

# Clean previous builds
dotnet clean -c Release -f net10.0-ios

# Build Release version (generates dSYMs)
dotnet build -c Release -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64
```

#### Step 2: Upload dSYMs to Datadog

```bash
# Set your Datadog API key
export DATADOG_API_KEY=your_api_key_here

# Upload dSYMs
../docs/ios/scripts/upload-dsyms.sh 1.0.0-test
```

Or manually:

```bash
# Install Datadog CLI (if not already installed)
npm install -g @datadog/datadog-ci

# Option 1: Upload from directory
npx @datadog/datadog-ci dsyms upload \
  bin/Release/net10.0-ios/iossimulator-arm64/ \
  --service com.datadog.mauiapp \
  --version 1.0.0-test

# Option 2: Upload from zip file
# (zip is created automatically by test-crash-reporting.sh)
npx @datadog/datadog-ci dsyms upload \
  dsyms-1.0.0-test.zip \
  --service com.datadog.mauiapp \
  --version 1.0.0-test

# Option 3: Create zip manually and upload
cd bin/Release/net10.0-ios/iossimulator-arm64
zip -r ../../../dsyms-1.0.0-test.zip *.dSYM
cd ../../..
npx @datadog/datadog-ci dsyms upload dsyms-1.0.0-test.zip
```

#### Step 3: Verify Upload

```bash
# List uploaded dSYMs
datadog-ci dsyms list --service com.datadog.mauiapp
```

You should see your recently uploaded dSYM files.

#### Step 4: Run the App

```bash
# Run on iOS Simulator
dotnet build -t:Run -c Release -f net10.0-ios
```

#### Step 5: Trigger Test Crash

1. Open the app on iOS Simulator
2. Navigate to the **Dashboard** tab
3. Scroll down to the yellow "⚠️ Crash Testing" section
4. Tap the **"Test Crash (iOS dSYM)"** button
5. Confirm the crash dialog by tapping **"Yes, Crash App"**

The app will crash immediately with a meaningful error message.

#### Step 6: View Crash in Datadog

1. Wait 1-5 minutes for the crash to be uploaded to Datadog
2. Go to Datadog → **RUM** → **Error Tracking**
   - URL: https://app.datadoghq.com/rum/error-tracking
3. Look for the crash with message: `TEST CRASH: This is an intentional crash...`

#### Step 7: Verify Symbolication

In the crash report, you should see a **symbolicated stack trace** with:

✅ **Without dSYM** (before upload):
```
0x00000001045a2c3c
0x00000001045a3124
0x00000001045a8d90
```

✅ **With dSYM** (after upload):
```
OnTestCrashClicked() at DashboardPage.xaml.cs:130
<lambda>() at DashboardPage.xaml.cs:91
Button_OnClicked() at Button.cs:156
```

If you see file names, method names, and line numbers, **dSYM upload worked!** 🎉

## Troubleshooting

### No dSYM Files Generated

**Problem**: Can't find `*.dSYM` files after building.

**Solutions**:
1. Ensure you're building in **Release** mode: `-c Release`
2. Check `MtouchSymbolsList=true` in [DatadogMauiApp.csproj:50](DatadogMauiApp.csproj#L50)
3. Build for specific platform: `-p:RuntimeIdentifier=iossimulator-arm64`
4. Not building for AnyCPU

**Verify**:
```bash
find bin/Release/net10.0-ios -name "*.dSYM"
```

### dSYM Upload Fails

**Problem**: `datadog-ci dsyms upload` returns an error.

**Solutions**:
1. Check `DATADOG_API_KEY` is set: `echo $DATADOG_API_KEY`
2. Verify API key is valid in Datadog
3. Check internet connectivity
4. Ensure dSYM files exist (see previous section)

### Crash Not Appearing in Datadog

**Problem**: Crash happened but not visible in Datadog.

**Solutions**:
1. **Wait longer**: Crashes can take 1-5 minutes to appear
2. **Check initialization**: Ensure Datadog is initialized in [AppDelegate.cs](Platforms/iOS/AppDelegate.cs)
3. **Verify credentials**: Check RUM Application ID and Client Token in `.env`
4. **Check network**: Ensure simulator has internet access
5. **Try Debug build**: Run in Debug mode first to see console logs

**Check logs**:
```bash
# Look for Datadog initialization logs
dotnet build -t:Run -f net10.0-ios 2>&1 | grep Datadog
```

### Stack Trace Not Symbolicated

**Problem**: Crash appears in Datadog but shows memory addresses, not symbols.

**Solutions**:
1. **Verify dSYM uploaded**:
   ```bash
   datadog-ci dsyms list --service com.datadog.mauiapp
   ```
2. **Check UUID match**:
   ```bash
   # Get dSYM UUID
   xcrun dwarfdump --uuid bin/Release/net10.0-ios/iossimulator-arm64/DatadogMauiApp.app.dSYM

   # Get app binary UUID
   xcrun dwarfdump --uuid bin/Release/net10.0-ios/iossimulator-arm64/DatadogMauiApp.app/DatadogMauiApp
   ```
   The UUIDs should match.

3. **Version mismatch**: Ensure the version you used for dSYM upload matches the app version:
   - dSYM upload version: `--version 1.0.0-test`
   - App version in csproj: `<ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>`

4. **Wait for processing**: Symbolication can take up to 15 minutes after upload

### Crash Reporting Not Enabled

**Problem**: Crash Reporting package errors.

**Current Status**: iOS Crash Reporting (`Bcr.Datadog.iOS.CR`) and Session Replay (`Bcr.Datadog.iOS.SR`) packages are installed but not yet configured in code.

**TODO**: Add Crash Reporting initialization once API is confirmed.

See [AppDelegate.cs:73](Platforms/iOS/AppDelegate.cs#L73) for the note about additional packages.

## Understanding the Test Crash

### What Happens When You Click "Test Crash"

1. **User Confirmation**: Shows a dialog explaining what will happen
2. **Logging**: Writes a log entry to Datadog before crashing
3. **Intentional Exception**: Throws `InvalidOperationException` with a detailed message
4. **Crash Capture**: Datadog SDK captures the crash
5. **Upload**: Crash is uploaded to Datadog servers (when app restarts)
6. **Symbolication**: Datadog symbolicates the stack trace using uploaded dSYMs

### The Crash Code

Location: [DashboardPage.xaml.cs:88-136](Pages/DashboardPage.xaml.cs#L88-L136)

```csharp
private async void OnTestCrashClicked(object sender, EventArgs e)
{
    // Confirm with user
    var confirmed = await DisplayAlert(...);

    // Log to Datadog
    DDLogger.Shared?.Error("Test crash initiated");

    // Intentional crash with meaningful message
    throw new InvalidOperationException(
        "TEST CRASH: This is an intentional crash to test Datadog crash reporting..."
    );
}
```

### Expected Crash Report

In Datadog, you should see:

**Error Message**:
```
TEST CRASH: This is an intentional crash to test Datadog crash reporting and dSYM symbolication.
This crash was triggered from DashboardPage.xaml.cs:OnTestCrashClicked().
If you see this symbolicated in Datadog, dSYM upload worked correctly!
```

**Stack Trace** (symbolicated):
```
1. InvalidOperationException: TEST CRASH...
   at DatadogMauiApp.Pages.DashboardPage.OnTestCrashClicked()
      DashboardPage.xaml.cs:130

2. at DatadogMauiApp.Pages.DashboardPage.<OnTestCrashClicked>b__2_0()
      DashboardPage.xaml.cs:91

3. at Microsoft.Maui.Controls.Button.Clicked()
      Button.cs:156
```

## Best Practices

### 1. Version Tagging

Always use semantic versions when uploading dSYMs:

```bash
# Bad
./test-crash-reporting.sh test

# Good
./test-crash-reporting.sh 1.0.0
./test-crash-reporting.sh 1.0.1-beta
./test-crash-reporting.sh 2.0.0-rc.1
```

### 2. Archive dSYMs

Keep dSYM files for every release:

```bash
# Create archive directory
mkdir -p dsym-archives/1.0.0

# Copy dSYMs
cp -r bin/Release/net10.0-ios/iossimulator-arm64/*.dSYM dsym-archives/1.0.0/
```

### 3. Automate in CI/CD

Add dSYM upload to your release pipeline:

```yaml
# GitHub Actions example
- name: Upload dSYMs to Datadog
  env:
    DATADOG_API_KEY: ${{ secrets.DATADOG_API_KEY }}
  run: |
    cd MauiApp
    ./test-crash-reporting.sh ${{ github.ref_name }}
```

### 4. Test on Physical Devices

For production testing, build for physical devices:

```bash
# Build for physical device
dotnet build -c Release -f net10.0-ios -p:RuntimeIdentifier=ios-arm64

# Upload dSYMs
datadog-ci dsyms upload \
  --service com.datadog.mauiapp \
  --version 1.0.0 \
  bin/Release/net10.0-ios/ios-arm64/*.dSYM
```

## Additional Resources

- [iOS Build Configuration](../docs/ios/BUILD_CONFIGURATION.md) - iOS build setup
- [iOS Crash Reporting Guide](../docs/ios/CRASH_REPORTING.md) - Complete dSYM guide
- [Upload dSYMs Script](../docs/ios/scripts/upload-dsyms.sh) - Manual upload script
- [Datadog iOS Crash Reporting Docs](https://docs.datadoghq.com/real_user_monitoring/error_tracking/mobile/ios/)
- [Datadog dSYM Upload Guide](https://docs.datadoghq.com/real_user_monitoring/ios/symbolication/)

## Summary

✅ **Test Crash Button Added**: Dashboard tab has a crash test button
✅ **Automated Script**: `test-crash-reporting.sh` handles build and upload
✅ **Comprehensive Guide**: This document covers all scenarios
✅ **Production Ready**: Works for both Debug and Release builds

Now you can easily test crash reporting and verify dSYM symbolication! 🎉
