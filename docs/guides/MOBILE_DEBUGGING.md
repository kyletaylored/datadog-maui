# Mobile App Debugging Guide

## Viewing Android Logs

### Quick Commands

```bash
# View Datadog-specific logs (recommended)
make app-logs-android

# View all logs
make app-logs-android-all

# Clear logs before running
make app-logs-clear
```

### Manual ADB Commands

```bash
# Find your Android SDK path
# macOS: ~/Library/Android/sdk/platform-tools/adb
# Linux: ~/Android/Sdk/platform-tools/adb
# Windows: %LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe

# Clear logs first (recommended before each run)
adb logcat -c

# View Datadog initialization logs
adb logcat | grep "\[Datadog\]"

# View all console output from your app
adb logcat | grep "mono-stdout"

# View specific tags
adb logcat | grep -E "DatadogMauiApp|Datadog|mono"

# Save logs to file
adb logcat > android-logs.txt
```

### Using Android Studio

1. **Open Logcat:**
   - Bottom toolbar → **Logcat** tab
   - Or: View → Tool Windows → Logcat

2. **Filter options:**
   ```
   # In the filter box, try these:
   [Datadog]           # Shows Datadog initialization logs
   package:mine        # Shows only your app's logs
   tag:mono-stdout     # Shows C# Console.WriteLine output
   ```

3. **Log levels:**
   - **Verbose** - Most detailed
   - **Debug** - Debug info
   - **Info** - General info (default)
   - **Warn** - Warnings
   - **Error** - Errors only

## Viewing iOS Logs

### Using Xcode

1. **Run app from Xcode**
2. **View → Debug Area → Activate Console** (or Cmd+Shift+Y)
3. Look for `[Datadog]` prefixed messages

### Using Terminal

```bash
# View iOS simulator logs
xcrun simctl spawn booted log stream --predicate 'processImagePath contains "DatadogMauiApp"'

# Or use Console.app
# Applications → Utilities → Console
# Select your device/simulator on left
# Filter for "DatadogMauiApp"
```

## Debugging Datadog RUM Initialization

### Expected Log Output

When Datadog initializes correctly, you should see:

```
[Datadog] Initializing for Android
[Datadog] - Environment: local
[Datadog] - Client Token: pub699a12...c2bd
[Datadog] - RUM Application ID: REDACTED_APP_ID_3
[Datadog] Core SDK initialized
[Datadog] Logs enabled
[Datadog] NDK crash reports enabled
[Datadog] RUM enabled
[Datadog] Session Replay enabled
[Datadog] APM Tracing enabled
[Datadog] Successfully initialized for Android
```

### Common Issues

#### Issue 1: Placeholder Values in Logs

**Symptom:**
```
[Datadog] - Client Token: PLACEHOLDER_ANDROID_CLIENT_TOKEN
[Datadog] - RUM Application ID: PLACEHOLDER_ANDROID_APPLICATION_ID
```

**Cause:** Environment variables not set before build

**Solution:**

Option A - Export before building:
```bash
export DD_RUM_ANDROID_CLIENT_TOKEN="REDACTED_CLIENT_TOKEN_1"
export DD_RUM_ANDROID_APPLICATION_ID="REDACTED_APP_ID_3"
dotnet build -f net10.0-android
```

Option B - Add to your shell profile (~/.zshrc or ~/.bashrc):
```bash
# Add to ~/.zshrc
export DD_RUM_ANDROID_CLIENT_TOKEN="REDACTED_CLIENT_TOKEN_1"
export DD_RUM_ANDROID_APPLICATION_ID="REDACTED_APP_ID_3"
export DD_RUM_IOS_CLIENT_TOKEN="REDACTED_CLIENT_TOKEN_2"
export DD_RUM_IOS_APPLICATION_ID="REDACTED_APP_ID_2"

# Then reload
source ~/.zshrc
```

Option C - Set in IDE:
- **Visual Studio**: Project Properties → Debug → Environment Variables
- **VS Code**: Edit `.vscode/launch.json`:
  ```json
  {
    "configurations": [
      {
        "name": ".NET MAUI",
        "type": "coreclr",
        "request": "launch",
        "env": {
          "DD_RUM_ANDROID_CLIENT_TOKEN": "REDACTED_CLIENT_TOKEN_1",
          "DD_RUM_ANDROID_APPLICATION_ID": "REDACTED_APP_ID_3"
        }
      }
    ]
  }
  ```

#### Issue 2: No Datadog Logs at All

**Symptoms:**
- No `[Datadog]` logs appear
- App starts but no initialization messages

**Debugging steps:**

1. **Verify app is actually running:**
   ```bash
   adb logcat | grep "DatadogMauiApp"
   ```

2. **Check for exceptions:**
   ```bash
   adb logcat | grep -E "Exception|Error"
   ```

3. **Verify MainApplication.OnCreate is called:**
   Add more logging in [MainApplication.cs](MauiApp/Platforms/Android/MainApplication.cs):
   ```csharp
   public override void OnCreate()
   {
       Console.WriteLine("[DEBUG] MainApplication.OnCreate called");
       base.OnCreate();
       Console.WriteLine("[DEBUG] About to initialize Datadog");
       InitializeDatadog();
       Console.WriteLine("[DEBUG] Datadog initialization complete");
   }
   ```

4. **Check Datadog packages are installed:**
   ```bash
   cd MauiApp
   dotnet list package | grep Datadog
   ```

#### Issue 3: RUM Not Sending Data

**Symptoms:**
- Datadog initializes successfully (logs show success)
- But no sessions appear in Datadog UI

**Debugging steps:**

1. **Verify credentials are correct:**
   - Check [.env](.env) has correct tokens
   - Verify in Datadog UI: RUM → Applications → Select your app → Settings

2. **Check network connectivity:**
   ```bash
   # From emulator, test connection
   adb shell ping datadoghq.com
   ```

3. **Enable verbose logging:**
   Already enabled in [DatadogConfig.cs](MauiApp/Config/DatadogConfig.cs:69):
   ```csharp
   public const bool VerboseLogging = true;
   ```

4. **Check for errors after initialization:**
   ```bash
   adb logcat | grep -E "Datadog|DD_"
   ```

5. **Verify RUM in Datadog UI:**
   - Go to https://app.datadoghq.com/rum/list
   - Select `datadog-maui-android`
   - Check "Sessions" should show recent activity

## Quick Debugging Workflow

### Full Debug Cycle

```bash
# 1. Set environment variables (from .env file)
source ./set-mobile-env.sh

# 2. Clean previous build
make app-clean

# 3. Clear old logs
make app-logs-clear

# 4. Build and run
make app-build-android
make app-run-android

# 5. In another terminal, watch logs
make app-logs-android

# 6. Look for initialization messages
# Should see [Datadog] logs within first few seconds with actual credentials
```

### Alternative: Manual Environment Variable Export

If you prefer to set variables manually:

```bash
# 1. Export environment variables
export DD_RUM_ANDROID_CLIENT_TOKEN="REDACTED_CLIENT_TOKEN_1"
export DD_RUM_ANDROID_APPLICATION_ID="REDACTED_APP_ID_3"

# 2. Verify they're set
echo $DD_RUM_ANDROID_CLIENT_TOKEN
echo $DD_RUM_ANDROID_APPLICATION_ID

# 3. Then build
make app-clean
make app-build-android
```

### Verify Environment Variables Are Set

Add this to [MainApplication.cs](MauiApp/Platforms/Android/MainApplication.cs) temporarily:

```csharp
private void InitializeDatadog()
{
    try
    {
        // ADD THIS BLOCK FOR DEBUGGING
        var envToken = System.Environment.GetEnvironmentVariable("DD_RUM_ANDROID_CLIENT_TOKEN");
        var envAppId = System.Environment.GetEnvironmentVariable("DD_RUM_ANDROID_APPLICATION_ID");
        Console.WriteLine($"[DEBUG] ENV DD_RUM_ANDROID_CLIENT_TOKEN: {(string.IsNullOrEmpty(envToken) ? "NOT SET" : envToken.Substring(0, 10) + "...")}");
        Console.WriteLine($"[DEBUG] ENV DD_RUM_ANDROID_APPLICATION_ID: {(string.IsNullOrEmpty(envAppId) ? "NOT SET" : envAppId)}");
        Console.WriteLine($"[DEBUG] Config.ClientToken: {DatadogConfig.ClientToken.Substring(0, 10)}...");
        Console.WriteLine($"[DEBUG] Config.RumApplicationId: {DatadogConfig.RumApplicationId}");

        Console.WriteLine($"[Datadog] Initializing for Android");
        // ... rest of initialization
```

## Common Log Patterns

### Successful Initialization
```
[Datadog] Initializing for Android
[Datadog] - Environment: local
[Datadog] - Client Token: pub699a12...c2bd
[Datadog] - RUM Application ID: REDACTED_APP_ID_3
[Datadog] Core SDK initialized
[Datadog] Successfully initialized for Android
```

### Failed Initialization
```
[Datadog] Failed to initialize: <error message>
[Datadog] Stack trace: <stack trace>
```

### Placeholder Warning
```
[Datadog] - Client Token: PLACEHOLDER_ANDROID_CLIENT_TOKEN
```
**Action needed:** Export environment variables before building

## Environment Variable Priority

The app uses this priority order:

1. **Runtime Environment Variable** (highest priority)
   ```bash
   export DD_RUM_ANDROID_CLIENT_TOKEN="..."
   ```

2. **Fallback to Placeholder** (in DatadogConfig.cs)
   ```csharp
   private const string DefaultAndroidClientToken = "PLACEHOLDER_ANDROID_CLIENT_TOKEN";
   ```

To verify which is being used, check the logs for the actual token value.

## Recommended Development Setup

### 1. Add to Shell Profile

Add to `~/.zshrc` (or `~/.bashrc`):

```bash
# Datadog RUM Credentials
export DD_RUM_ANDROID_CLIENT_TOKEN="REDACTED_CLIENT_TOKEN_1"
export DD_RUM_ANDROID_APPLICATION_ID="REDACTED_APP_ID_3"
export DD_RUM_IOS_CLIENT_TOKEN="REDACTED_CLIENT_TOKEN_2"
export DD_RUM_IOS_APPLICATION_ID="REDACTED_APP_ID_2"
```

Then reload: `source ~/.zshrc`

### 2. Verify Before Building

```bash
# Check env vars are set
echo $DD_RUM_ANDROID_CLIENT_TOKEN
echo $DD_RUM_ANDROID_APPLICATION_ID
```

### 3. Use Makefile Commands

```bash
# Clear logs, build, run, and watch
make app-logs-clear && make app-run-android
# In another terminal:
make app-logs-android
```

## Additional Resources

- [Android Logcat Documentation](https://developer.android.com/tools/logcat)
- [Datadog Android RUM Setup](https://docs.datadoghq.com/real_user_monitoring/mobile_and_tv_monitoring/android/)
- [Datadog Config](../../MauiApp/Config/DatadogConfig.cs)
- [Android MainApplication](../../MauiApp/Platforms/Android/MainApplication.cs)

---

**Quick Links:**
- [RUM Configuration Guide](RUM_CONFIGURATION.md)
- [Main README](../../README.md)
