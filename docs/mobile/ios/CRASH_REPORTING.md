# iOS dSYM Crash Reporting with Datadog

This guide explains how to capture, symbolicate, and upload iOS crash logs (dSYMs) to Datadog for crash reporting.

## What are dSYM Files?

**dSYM (Debug Symbol)** files contain debug information that maps machine code back to your source code. When an iOS app crashes, the stack trace contains memory addresses. dSYM files are needed to "symbolicate" these addresses back into readable function names, file names, and line numbers.

Example:
```
Without dSYM: 0x00000001045a2c3c
With dSYM:    AppDelegate.FinishedLaunching() at AppDelegate.cs:15
```

## Prerequisites

1. **Datadog iOS SDK** - Already installed via NuGet packages
2. **Datadog API Key** - For uploading dSYMs
3. **Release Build** - dSYMs are generated during Release builds
4. **Crash Reporting Enabled** - In Datadog initialization

## Step 1: Build Configuration

The project is already configured to generate dSYMs for Release builds. See [DatadogMauiApp.csproj:43-51](DatadogMauiApp.csproj#L43-L51):

```xml
<!-- iOS dSYM Configuration for Crash Reporting -->
<PropertyGroup Condition="'$(Configuration)' == 'Release' AND '$(TargetFramework)' == 'net10.0-ios'">
    <!-- Generate dSYM files for crash symbolication -->
    <MtouchDebug>false</MtouchDebug>
    <MtouchSymbolsList>true</MtouchSymbolsList>
    <!-- AOT compilation generates better dSYMs -->
    <MtouchLink>Full</MtouchLink>
    <UseInterpreter>false</UseInterpreter>
</PropertyGroup>
```

### Key Properties:

- **`MtouchDebug=false`**: Disables debug mode (required for Release)
- **`MtouchSymbolsList=true`**: Generates the symbols list
- **`MtouchLink=Full`**: Full AOT compilation (generates comprehensive debug symbols)
- **`UseInterpreter=false`**: Disables interpreter mode (AOT produces better dSYMs)

## Step 2: Enable Crash Reporting in iOS

Uncomment and configure Datadog initialization in [AppDelegate.cs](Platforms/iOS/AppDelegate.cs):

```csharp
using Datadog.iOS.ObjC;
using DatadogMauiApp.Config;

public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
{
    InitializeDatadog();
    return base.FinishedLaunching(application, launchOptions);
}

private void InitializeDatadog()
{
    try
    {
        // Initialize the Datadog SDK
        var config = new DDConfiguration(
            DatadogConfig.ClientToken,
            DatadogConfig.Environment
        );

        config.Service = DatadogConfig.ServiceName;
        config.Site = DDDatadogSite.Us1; // or Us3, Us5, Eu1, etc.

        DDDatadog.Initialize(config, DDTrackingConsent.Granted);

        // Enable Crash Reporting (CRITICAL for dSYM)
        DDCrashReporter.Enable();

        // Enable RUM (Real User Monitoring)
        var rumConfig = new DDRUMConfiguration(DatadogConfig.RumApplicationId);
        rumConfig.SessionSampleRate = DatadogConfig.SessionSampleRate;
        DDRUM.Enable(rumConfig);

        // Enable Logs
        DDLogs.Enable(new DDLogsConfiguration(null));

        // Enable Trace
        DDTrace.Enable(new DDTraceConfiguration());

        Console.WriteLine("[Datadog] Successfully initialized for iOS with Crash Reporting");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Datadog] Failed to initialize: {ex.Message}");
    }
}
```

**Key Line**: `DDCrashReporter.Enable();` - This enables crash reporting and will capture crashes.

## Step 3: Build Release Version

Build the app in Release mode to generate dSYM files:

```bash
cd MauiApp

# For physical device (required for crash testing)
dotnet build -c Release -f net10.0-ios -p:RuntimeIdentifier=ios-arm64

# For App Store distribution
dotnet publish -c Release -f net10.0-ios -p:RuntimeIdentifier=ios-arm64
```

## Step 4: Locate dSYM Files

After building, dSYM files are located in:

```bash
# Find all dSYM files
find bin/Release/net10.0-ios -name "*.dSYM"

# Typical locations:
bin/Release/net10.0-ios/ios-arm64/DatadogMauiApp.app.dSYM
bin/Release/net10.0-ios/ios-arm64/Symbols/DatadogMauiApp.app.dSYM
```

The dSYM is actually a directory bundle containing:
```
DatadogMauiApp.app.dSYM/
└── Contents/
    ├── Info.plist
    └── Resources/
        └── DWARF/
            └── DatadogMauiApp  ← This is the actual symbol file
```

## Step 5: Upload dSYMs to Datadog

### Option 1: Using Datadog CLI (Recommended)

Install Datadog CLI:
```bash
npm install -g @datadog/datadog-ci
```

Upload dSYMs:
```bash
# Set your Datadog API key
export DATADOG_API_KEY=your_api_key_here

# Upload all dSYMs from the build
datadog-ci dsyms upload \
  --service com.datadog.mauiapp \
  --repository-url https://github.com/your-org/your-repo \
  bin/Release/net10.0-ios/ios-arm64/*.dSYM
```

### Option 2: Using Datadog API Directly

```bash
# Get UUID of the dSYM
xcrun dwarfdump --uuid bin/Release/net10.0-ios/ios-arm64/DatadogMauiApp.app.dSYM

# Compress the dSYM
cd bin/Release/net10.0-ios/ios-arm64
zip -r DatadogMauiApp.dSYM.zip DatadogMauiApp.app.dSYM

# Upload to Datadog
curl -X POST "https://api.datadoghq.com/api/v2/debugger/upload" \
  -H "DD-API-KEY: ${DATADOG_API_KEY}" \
  -F "file=@DatadogMauiApp.dSYM.zip" \
  -F "service=com.datadog.mauiapp" \
  -F "version=1.0" \
  -F "type=ios"
```

### Option 3: Automatic Upload via CI/CD

Add to your CI/CD pipeline (GitHub Actions, Azure Pipelines, etc.):

```yaml
# Example GitHub Actions workflow
- name: Upload dSYMs to Datadog
  env:
    DATADOG_API_KEY: ${{ secrets.DATADOG_API_KEY }}
  run: |
    npm install -g @datadog/datadog-ci
    datadog-ci dsyms upload \
      --service com.datadog.mauiapp \
      --repository-url ${{ github.repositoryUrl }} \
      MauiApp/bin/Release/net10.0-ios/ios-arm64/*.dSYM
```

## Step 6: Verify Upload

Check that dSYMs were uploaded successfully:

```bash
# Using Datadog CLI
datadog-ci dsyms list --service com.datadog.mauiapp

# Or check in Datadog UI:
# 1. Go to Error Tracking > Settings
# 2. Navigate to Symbol Files tab
# 3. Search for your app's bundle ID: com.datadog.mauiapp
```

## Step 7: Test Crash Reporting

### Create a Test Crash

Add a crash button to your app for testing:

```csharp
// In your XAML page
<Button Text="Test Crash" Clicked="OnCrashClicked" />

// In your code-behind
private void OnCrashClicked(object sender, EventArgs e)
{
    // Force crash for testing
    throw new Exception("Test crash for Datadog dSYM symbolication");
}
```

### View Crash in Datadog

1. Trigger the crash in your app
2. Wait 1-5 minutes for the crash to upload
3. Go to Datadog → **Error Tracking** → **Issues**
4. You should see the crash with symbolicated stack trace

**Without dSYM**:
```
0x00000001045a2c3c
0x00000001045a3124
0x00000001045a8d90
```

**With dSYM**:
```
OnCrashClicked() at MainPage.xaml.cs:42
Button_OnClicked() at Button.cs:156
UIApplication.Main() at UIApplication.cs:89
```

## Troubleshooting

### dSYM Files Not Generated

**Problem**: No `.dSYM` files found after building.

**Solutions**:
1. Ensure you're building in **Release** mode: `-c Release`
2. Check `MtouchSymbolsList=true` in csproj
3. Use full AOT compilation: `MtouchLink=Full`
4. Build for physical device, not simulator: `RuntimeIdentifier=ios-arm64`

### Upload Fails: UUID Mismatch

**Problem**: Datadog rejects dSYM due to UUID mismatch.

**Solutions**:
1. Verify dSYM UUID matches app binary:
   ```bash
   # Get dSYM UUID
   xcrun dwarfdump --uuid DatadogMauiApp.app.dSYM

   # Get app binary UUID
   xcrun dwarfdump --uuid DatadogMauiApp.app/DatadogMauiApp
   ```
2. Ensure you're uploading the dSYM from the exact same build as the distributed app
3. Clean and rebuild if UUIDs don't match

### Crashes Not Symbolicated

**Problem**: Crashes appear in Datadog but stack traces show memory addresses, not symbols.

**Solutions**:
1. Verify dSYM was uploaded:
   ```bash
   datadog-ci dsyms list --service com.datadog.mauiapp
   ```
2. Check that the version in dSYM matches the app version
3. Ensure `DDCrashReporter.Enable()` is called during initialization
4. Wait up to 15 minutes for symbolication (it's not instant)

### Simulator Crashes Not Captured

**Problem**: Crashes on simulator don't appear in Datadog.

**Reason**: dSYM files are only generated for physical device builds (`ios-arm64`), not simulator builds (`iossimulator-arm64`).

**Solution**: Test crash reporting on a physical device or TestFlight build.

## Best Practices

### 1. Automate dSYM Upload in CI/CD

Add dSYM upload to your release pipeline:
```bash
# After building Release version
datadog-ci dsyms upload \
  --service com.datadog.mauiapp \
  --version $VERSION \
  bin/Release/net10.0-ios/ios-arm64/*.dSYM
```

### 2. Store dSYMs for Archive Builds

Keep dSYM files for every version you distribute:
```bash
# Create archive directory
mkdir -p archives/$VERSION

# Copy dSYMs
cp -r bin/Release/net10.0-ios/ios-arm64/*.dSYM archives/$VERSION/
```

### 3. Version Tagging

Match dSYM versions to app versions:
```xml
<!-- In DatadogMauiApp.csproj -->
<ApplicationDisplayVersion>1.2.3</ApplicationDisplayVersion>
<ApplicationVersion>123</ApplicationVersion>
```

Use the same version when uploading dSYMs:
```bash
datadog-ci dsyms upload \
  --service com.datadog.mauiapp \
  --version 1.2.3 \
  *.dSYM
```

### 4. Monitor Upload Status

Add verification to your CI/CD:
```bash
# Upload dSYMs
datadog-ci dsyms upload *.dSYM

# Verify upload succeeded
if [ $? -eq 0 ]; then
  echo "✅ dSYM upload successful"
else
  echo "❌ dSYM upload failed"
  exit 1
fi
```

## Configuration Files Reference

### dSYM Build Configuration
- [DatadogMauiApp.csproj](DatadogMauiApp.csproj#L43-L51) - Release build settings

### Crash Reporting Code
- [AppDelegate.cs](Platforms/iOS/AppDelegate.cs) - Datadog initialization (commented out)

### Environment Configuration
- `.env` file - Add these variables:
  ```bash
  DD_RUM_IOS_CLIENT_TOKEN=your_token
  DD_RUM_IOS_APPLICATION_ID=your_app_id
  DATADOG_API_KEY=your_api_key  # For dSYM upload
  ```

## Resources

- [Datadog iOS Crash Reporting Docs](https://docs.datadoghq.com/real_user_monitoring/error_tracking/mobile/ios/)
- [Datadog dSYM Upload Guide](https://docs.datadoghq.com/real_user_monitoring/ios/symbolication/)
- [Datadog CLI Documentation](https://github.com/DataDog/datadog-ci)
- [Apple dSYM Documentation](https://developer.apple.com/documentation/xcode/building-your-app-to-include-debugging-information)
- [iOS Binding Repository](https://github.com/brunck/datadog-dotnet-mobile-sdk-bindings)

## Summary Checklist

- [ ] Release build configured to generate dSYMs
- [ ] `DDCrashReporter.Enable()` called in AppDelegate
- [ ] Build app in Release mode for physical device
- [ ] Locate dSYM files in build output
- [ ] Install Datadog CLI: `npm install -g @datadog/datadog-ci`
- [ ] Set `DATADOG_API_KEY` environment variable
- [ ] Upload dSYMs: `datadog-ci dsyms upload *.dSYM`
- [ ] Verify upload: `datadog-ci dsyms list`
- [ ] Test crash on physical device
- [ ] Confirm symbolicated crash appears in Datadog Error Tracking
- [ ] Add dSYM upload to CI/CD pipeline

---

**Next Steps**: Once iOS builds are running on a physical device, test crash reporting and verify that stack traces are properly symbolicated in Datadog.
