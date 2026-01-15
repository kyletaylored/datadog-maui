# Mobile App Build Configuration

## Overview

The mobile app (Android & iOS) now automatically loads Datadog RUM credentials from your [.env](.env) file during build using MSBuild integration. No need to manually export environment variables!

## How It Works

### 1. Makefile Integration

The Makefile commands automatically source [set-mobile-env.sh](set-mobile-env.sh) and pass credentials to MSBuild:

```makefile
app-build-android:
    source ./set-mobile-env.sh && \
    cd MauiApp && \
    dotnet build -f net10.0-android \
        -p:AndroidClientToken=env:DD_RUM_ANDROID_CLIENT_TOKEN \
        -p:AndroidApplicationId=env:DD_RUM_ANDROID_APPLICATION_ID
```

### 2. MSBuild Property Configuration

The [DatadogMauiApp.csproj](MauiApp/DatadogMauiApp.csproj) file defines custom MSBuild properties:

```xml
<PropertyGroup>
    <!-- Android RUM credentials (from MSBuild properties or environment) -->
    <AndroidClientToken Condition="'$(AndroidClientToken)' == ''">$(DD_RUM_ANDROID_CLIENT_TOKEN)</AndroidClientToken>
    <AndroidApplicationId Condition="'$(AndroidApplicationId)' == ''">$(DD_RUM_ANDROID_APPLICATION_ID)</AndroidApplicationId>

    <!-- iOS RUM credentials (from MSBuild properties or environment) -->
    <IosClientToken Condition="'$(IosClientToken)' == ''">$(DD_RUM_IOS_CLIENT_TOKEN)</IosClientToken>
    <IosApplicationId Condition="'$(IosApplicationId)' == ''">$(DD_RUM_IOS_APPLICATION_ID)</IosApplicationId>
</PropertyGroup>
```

### 3. Build-Time Environment Variable Injection

Custom MSBuild targets set environment variables before compilation:

```xml
<Target Name="SetDatadogAndroidEnv" BeforeTargets="BeforeBuild;CoreCompile"
        Condition="'$(TargetFramework)' == 'net10.0-android' AND '$(AndroidClientToken)' != ''">
    <SetEnvironmentVariable Name="DD_RUM_ANDROID_CLIENT_TOKEN" Value="$(AndroidClientToken)" />
    <SetEnvironmentVariable Name="DD_RUM_ANDROID_APPLICATION_ID" Value="$(AndroidApplicationId)" />
</Target>
```

### 4. Runtime Configuration

The [DatadogConfig.cs](MauiApp/Config/DatadogConfig.cs) reads from environment variables at runtime:

```csharp
public static string ClientToken
{
    get
    {
#if ANDROID
        var envToken = System.Environment.GetEnvironmentVariable("DD_RUM_ANDROID_CLIENT_TOKEN");
        return !string.IsNullOrEmpty(envToken) ? envToken : DefaultAndroidClientToken;
#elif IOS
        var envToken = System.Environment.GetEnvironmentVariable("DD_RUM_IOS_CLIENT_TOKEN");
        return !string.IsNullOrEmpty(envToken) ? envToken : DefaultIosClientToken;
#endif
    }
}
```

## Usage

### Simple Build (Recommended)

Just use the Makefile commands - credentials load automatically:

```bash
# Android
make app-build-android
make app-run-android

# iOS
make app-build-ios
make app-run-ios
```

### Manual Build (Advanced)

If you need to build manually without the Makefile:

**Option A: Source env script first**
```bash
source ./set-mobile-env.sh
cd MauiApp
dotnet build -f net10.0-android
```

**Option B: Pass credentials directly**
```bash
cd MauiApp
dotnet build -f net10.0-android \
    -p:AndroidClientToken=env:DD_RUM_ANDROID_CLIENT_TOKEN \
    -p:AndroidApplicationId=env:DD_RUM_ANDROID_APPLICATION_ID
```

**Option C: Use environment variables directly**
```bash
export DD_RUM_ANDROID_CLIENT_TOKEN="REDACTED_CLIENT_TOKEN_1"
export DD_RUM_ANDROID_APPLICATION_ID="REDACTED_APP_ID_3"
cd MauiApp
dotnet build -f net10.0-android
```

### IDE Build Configuration

#### Visual Studio

1. Right-click project → **Properties**
2. Go to **Debug** → **Debug Properties**
3. Under **Environment Variables**, add:
   ```
   DD_RUM_ANDROID_CLIENT_TOKEN=REDACTED_CLIENT_TOKEN_1
   DD_RUM_ANDROID_APPLICATION_ID=REDACTED_APP_ID_3
   ```

#### VS Code

Edit [.vscode/launch.json](.vscode/launch.json):

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET MAUI Android",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "load-env",
      "env": {
        "DD_RUM_ANDROID_CLIENT_TOKEN": "${env:DD_RUM_ANDROID_CLIENT_TOKEN}",
        "DD_RUM_ANDROID_APPLICATION_ID": "${env:DD_RUM_ANDROID_APPLICATION_ID}"
      }
    }
  ]
}
```

#### JetBrains Rider

1. **Run** → **Edit Configurations**
2. Select your MAUI configuration
3. Add to **Environment Variables**:
   ```
   DD_RUM_ANDROID_CLIENT_TOKEN=REDACTED_CLIENT_TOKEN_1
   DD_RUM_ANDROID_APPLICATION_ID=REDACTED_APP_ID_3
   ```

## Verification

### Check Build Logs

Look for MSBuild messages during build:

```
[MSBuild] 🐕 Setting Android RUM credentials from build properties
[MSBuild]   Client Token: REDACTED_CLIENT_TOKEN_1
[MSBuild]   Application ID: REDACTED_APP_ID_3
```

### Check Runtime Logs

After building and running, check Android logs:

```bash
make app-logs-android
```

**Success (actual credentials):**
```
[Datadog] - Client Token: pub699a12...c2bd
[Datadog] - RUM Application ID: REDACTED_APP_ID_3
```

**Failure (placeholders):**
```
[Datadog] - Client Token: PLACEHOLDER_ANDROID_CLIENT_TOKEN
[Datadog] - RUM Application ID: PLACEHOLDER_ANDROID_APPLICATION_ID
```

## How Credentials Flow

```
┌─────────────────────────────────────────────────────────────┐
│ 1. .env file (gitignored)                                   │
│    DD_RUM_ANDROID_CLIENT_TOKEN=pub699a12...                 │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. Makefile sources set-mobile-env.sh                       │
│    Exports environment variables                            │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. dotnet build command with MSBuild properties             │
│    -p:AndroidClientToken=env:DD_RUM_ANDROID_CLIENT_TOKEN    │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. MSBuild reads property and sets environment variable     │
│    SetEnvironmentVariable task in .csproj                   │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. C# code reads environment variable at runtime            │
│    DatadogConfig.ClientToken property                       │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. Datadog SDK initializes with credentials                 │
│    MainApplication.cs / AppDelegate.cs                      │
└─────────────────────────────────────────────────────────────┘
```

## Troubleshooting

### Issue: Placeholder values in logs

**Problem:**
```
[Datadog] - Client Token: PLACEHOLDER_ANDROID_CLIENT_TOKEN
```

**Solutions:**

1. **Verify .env file exists and has values:**
   ```bash
   cat .env | grep DD_RUM_ANDROID
   ```

2. **Check if set-mobile-env.sh loads correctly:**
   ```bash
   source ./set-mobile-env.sh
   echo $DD_RUM_ANDROID_CLIENT_TOKEN
   ```

3. **Clean and rebuild:**
   ```bash
   make app-clean
   make app-build-android
   ```

4. **Check MSBuild output:**
   ```bash
   make app-build-android 2>&1 | grep MSBuild
   ```

### Issue: SetEnvironmentVariable task not found

**Problem:**
```
error MSB4036: The "SetEnvironmentVariable" task was not found
```

**Solution:**

The `SetEnvironmentVariable` task requires MSBuild extensions. Alternatively, use the simpler approach with direct environment variable passing from the shell (which the Makefile already does via `source ./set-mobile-env.sh`).

### Issue: Credentials work in Makefile but not in IDE

**Problem:** `make app-run-android` works but running from Visual Studio/Rider shows placeholders.

**Solution:** Configure environment variables in your IDE settings (see IDE Build Configuration section above).

## CI/CD Integration

### GitHub Actions

```yaml
- name: Build Android App
  env:
    DD_RUM_ANDROID_CLIENT_TOKEN: ${{ secrets.DD_RUM_ANDROID_CLIENT_TOKEN }}
    DD_RUM_ANDROID_APPLICATION_ID: ${{ secrets.DD_RUM_ANDROID_APPLICATION_ID }}
  run: |
    cd MauiApp
    dotnet build -f net10.0-android \
      -p:AndroidClientToken="${DD_RUM_ANDROID_CLIENT_TOKEN}" \
      -p:AndroidApplicationId="${DD_RUM_ANDROID_APPLICATION_ID}"
```

### GitLab CI

```yaml
build-android:
  variables:
    DD_RUM_ANDROID_CLIENT_TOKEN: $DD_RUM_ANDROID_CLIENT_TOKEN
    DD_RUM_ANDROID_APPLICATION_ID: $DD_RUM_ANDROID_APPLICATION_ID
  script:
    - cd MauiApp
    - dotnet build -f net10.0-android
      -p:AndroidClientToken="${DD_RUM_ANDROID_CLIENT_TOKEN}"
      -p:AndroidApplicationId="${DD_RUM_ANDROID_APPLICATION_ID}"
```

## Best Practices

1. **Always use Makefile commands** - They handle environment loading automatically
2. **Never commit .env file** - Keep credentials out of source control
3. **Use separate credentials per environment** - Different tokens for dev/staging/prod
4. **Verify credentials in logs** - Check that real tokens (not placeholders) are being used
5. **Clean build when changing credentials** - Run `make app-clean` after updating .env

## Related Documentation

- [Mobile Debugging Guide](MOBILE_DEBUGGING.md) - How to view logs and debug initialization
- [RUM Configuration Guide](RUM_CONFIGURATION.md) - Overall RUM setup across all platforms
- [Main README](../../README.md) - Project overview

---

**Status:** ✅ Configured

**Last Updated:** 2025-12-31

**Features:**
- ✅ Automatic credential loading from .env
- ✅ MSBuild property integration
- ✅ Build-time environment variable injection
- ✅ Runtime credential reading
- ✅ IDE-friendly configuration
- ✅ CI/CD ready
