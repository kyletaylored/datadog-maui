# RUM (Real User Monitoring) Configuration

## Overview

This project uses Datadog RUM to monitor user interactions across three platforms:

- **Web**: Browser-based dashboard (index.html)
- **Android**: Native Android app
- **iOS**: Native iOS app

All RUM credentials are managed via environment variables to keep sensitive values out of source control.

## Configuration Files

### Environment Variables (.env)

**File:** [.env](.env) (gitignored - use [.env.example](.env.example) as template)

```bash
# DataDog RUM (Real User Monitoring) - Web
DD_RUM_WEB_CLIENT_TOKEN=pub37e71f...
DD_RUM_WEB_APPLICATION_ID=6af6b082...

# DataDog RUM - Android
DD_RUM_ANDROID_CLIENT_TOKEN=pub699a12...
DD_RUM_ANDROID_APPLICATION_ID=7658abf2...

# DataDog RUM - iOS
DD_RUM_IOS_CLIENT_TOKEN=pub9b087a...
DD_RUM_IOS_APPLICATION_ID=6de15c6c...
```

### Platform-Specific Configuration

#### 1. Web (Browser)

**File:** [Api/wwwroot/index.html](Api/wwwroot/index.html)

RUM credentials are **injected at Docker build time** from environment variables.

**Source (with placeholders):**

```html
<script>
  window.DD_RUM.onReady(function () {
    window.DD_RUM.init({
      clientToken: "PLACEHOLDER_CLIENT_TOKEN",
      applicationId: "PLACEHOLDER_APPLICATION_ID",
      site: "PLACEHOLDER_SITE",
      service: "PLACEHOLDER_SERVICE",
      env: "PLACEHOLDER_ENV",
      version: "PLACEHOLDER_VERSION",
      sessionSampleRate: 100,
      sessionReplaySampleRate: 100,
      trackBfcacheViews: true,
      defaultPrivacyLevel: "mask-user-input",
    });
  });
</script>
```

**Build Process ([Api/Dockerfile](Api/Dockerfile)):**

```dockerfile
# Inject RUM credentials into index.html at build time
RUN if [ -f wwwroot/index.html ] && [ -n "$DD_RUM_WEB_CLIENT_TOKEN" ] && [ -n "$DD_RUM_WEB_APPLICATION_ID" ]; then \
      sed -i "s/PLACEHOLDER_CLIENT_TOKEN/${DD_RUM_WEB_CLIENT_TOKEN}/" wwwroot/index.html && \
      sed -i "s/PLACEHOLDER_APPLICATION_ID/${DD_RUM_WEB_APPLICATION_ID}/" wwwroot/index.html && \
      sed -i "s/PLACEHOLDER_SITE/${DD_SITE}/" wwwroot/index.html && \
      sed -i "s/PLACEHOLDER_SERVICE/${DD_RUM_WEB_SERVICE}/" wwwroot/index.html && \
      sed -i "s/PLACEHOLDER_VERSION/${DD_VERSION}/" wwwroot/index.html && \
      sed -i "s/PLACEHOLDER_ENV/${DD_ENV}/" wwwroot/index.html && \
      echo "✅ RUM credentials injected into index.html"; \
    else \
      echo "⚠️  Skipping RUM injection (file not found or credentials not provided)"; \
    fi
```

**Docker Compose ([docker-compose.yml](docker-compose.yml)):**

```yaml
api:
  build:
    args:
      - DD_RUM_WEB_CLIENT_TOKEN=${DD_RUM_WEB_CLIENT_TOKEN}
      - DD_RUM_WEB_APPLICATION_ID=${DD_RUM_WEB_APPLICATION_ID}
```

#### 2. Android & iOS (Mobile Apps)

Mobile apps use a sophisticated **3-tier credential loading system**:

1. **Embedded Config File** (highest priority) - generated at build time
2. **Environment Variables** - runtime environment
3. **Default Placeholders** (lowest priority) - compile-time constants

##### Build-Time: MSBuild Target Generation

**File:** [MauiApp/DatadogMauiApp.csproj](../../MauiApp/DatadogMauiApp.csproj)

The .csproj includes custom MSBuild targets that generate platform-specific config files during build:

```xml
<!-- Android RUM credentials from environment or MSBuild properties -->
<PropertyGroup>
  <AndroidClientToken Condition="'$(AndroidClientToken)' == ''">$(DD_RUM_ANDROID_CLIENT_TOKEN)</AndroidClientToken>
  <AndroidApplicationId Condition="'$(AndroidApplicationId)' == ''">$(DD_RUM_ANDROID_APPLICATION_ID)</AndroidApplicationId>
</PropertyGroup>

<!-- Generate Android config file before build -->
<Target Name="GenerateAndroidRumConfig" BeforeTargets="BeforeBuild" Condition="'$(TargetFramework)' == 'net10.0-android'">
  <WriteLinesToFile
    File="$(MSBuildProjectDirectory)/Platforms/Android/datadog-rum.config"
    Lines="DD_RUM_ANDROID_CLIENT_TOKEN=$(AndroidClientToken);DD_RUM_ANDROID_APPLICATION_ID=$(AndroidApplicationId)"
    Overwrite="true" />
</Target>

<!-- Embed config file as manifest resource -->
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-android'">
  <EmbeddedResource Include="Platforms/Android/datadog-rum.config" />
</ItemGroup>
```

Similar targets exist for iOS.

##### Runtime: Dynamic Credential Loading

**File:** [MauiApp/Config/DatadogConfig.cs](../../MauiApp/Config/DatadogConfig.cs)

At runtime, the app loads credentials using conditional compilation and reflection:

```csharp
public static class DatadogConfig
{
    // Fallback defaults (placeholders)
    private const string DefaultAndroidClientToken = "PLACEHOLDER_ANDROID_CLIENT_TOKEN";

    // Load credentials from embedded config file
    private static Dictionary<string, string> LoadCredentials()
    {
        var creds = new Dictionary<string, string>();

#if ANDROID
        var resourceName = "DatadogMauiApp.Platforms.Android.datadog-rum.config";
#elif IOS
        var resourceName = "DatadogMauiApp.Platforms.iOS.datadog-rum.config";
#endif

        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();
            // Parse KEY=VALUE format...
        }
        return creds;
    }

    // 3-tier priority: Embedded Config > Environment Variable > Placeholder
    public static string ClientToken
    {
        get
        {
#if ANDROID
            // Priority 1: Embedded config file
            var creds = LoadCredentials();
            if (creds.TryGetValue("DD_RUM_ANDROID_CLIENT_TOKEN", out var configToken))
                return configToken;

            // Priority 2: Environment variable
            var envToken = System.Environment.GetEnvironmentVariable("DD_RUM_ANDROID_CLIENT_TOKEN");
            return !string.IsNullOrEmpty(envToken) ? envToken : DefaultAndroidClientToken;
#elif IOS
            // Same pattern for iOS...
#endif
        }
    }
}
```

**Key Benefits:**

✅ **Build-time injection** - credentials embedded during compilation
✅ **No file I/O** - config loaded from embedded resources
✅ **Platform-specific** - separate credentials for Android/iOS
✅ **Secure** - no `.env` files distributed with app
✅ **CI/CD friendly** - reads from environment variables
✅ **Graceful degradation** - 3-tier fallback system

## Setup Instructions

### Initial Setup

1. **Copy environment template:**

   ```bash
   cp .env.example .env
   ```

2. **Get RUM credentials from Datadog:**
   - Go to [Datadog RUM Applications](https://app.datadoghq.com/rum/application/create)
   - Create three separate RUM applications:
     - **datadog-maui-web** (Browser, React)
     - **datadog-maui-android** (Android)
     - **datadog-maui-ios** (iOS)
   - Copy the Client Token and Application ID for each

3. **Update .env file:**

   ```bash
   # Edit .env with your actual credentials
   nano .env
   ```

4. **Verify .env is gitignored:**
   ```bash
   git check-ignore .env
   # Should output: .env
   ```

### Building with RUM Credentials

#### Web (Docker)

```bash
# Load environment variables
source .env

# Build Docker image (credentials injected at build time)
source ./set-git-metadata.sh
docker-compose build api

# Start containers
docker-compose up -d

# Verify injection worked
docker exec datadog-maui-api cat wwwroot/index.html | grep clientToken
# Should show: clientToken: 'pub37e71f...' (NOT 'PLACEHOLDER_CLIENT_TOKEN')
```

#### Android

The Android build process automatically generates a config file from environment variables:

```bash
# Set environment variables
export DD_RUM_ANDROID_CLIENT_TOKEN="pub699a12..."
export DD_RUM_ANDROID_APPLICATION_ID="7658abf2..."

# Build Android app
# The GenerateAndroidRumConfig target runs automatically
dotnet build -f net10.0-android

# Verify config file was generated
cat MauiApp/Platforms/Android/datadog-rum.config
# Should output:
# DD_RUM_ANDROID_CLIENT_TOKEN=pub699a12...
# DD_RUM_ANDROID_APPLICATION_ID=7658abf2...
```

**What happens during build:**

1. MSBuild reads `DD_RUM_ANDROID_CLIENT_TOKEN` from environment
2. `GenerateAndroidRumConfig` target creates `datadog-rum.config` file
3. File is embedded as `EmbeddedResource` in assembly
4. At runtime, `DatadogConfig.cs` loads credentials from embedded resource

#### iOS

The iOS build process works identically to Android:

```bash
# Set environment variables
export DD_RUM_IOS_CLIENT_TOKEN="pub9b087a..."
export DD_RUM_IOS_APPLICATION_ID="6de15c6c..."

# Build iOS app
# The GenerateIosRumConfig target runs automatically
dotnet build -f net10.0-ios

# Verify config file was generated
cat MauiApp/Platforms/iOS/datadog-rum.config
# Should output:
# DD_RUM_IOS_CLIENT_TOKEN=pub9b087a...
# DD_RUM_IOS_APPLICATION_ID=6de15c6c...
```

**Build output messages:**

```
[MSBuild] 🐕 Generating Android RUM config file...
[MSBuild]   Client Token: pub699a12...
[MSBuild]   Application ID: 7658abf2...
```

Or if credentials are missing:

```
[MSBuild] ⚠️  No Android RUM credentials found! App will use placeholder values.
```

## CI/CD Integration

### GitHub Actions

```yaml
name: Build and Deploy

on:
  push:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Set RUM credentials
        env:
          DD_RUM_WEB_CLIENT_TOKEN: ${{ secrets.DD_RUM_WEB_CLIENT_TOKEN }}
          DD_RUM_WEB_APPLICATION_ID: ${{ secrets.DD_RUM_WEB_APPLICATION_ID }}
          DD_RUM_WEB_SERVICE: ${{ secrets.DD_RUM_WEB_SERVICE }}
          DD_RUM_ANDROID_CLIENT_TOKEN: ${{ secrets.DD_RUM_ANDROID_CLIENT_TOKEN }}
          DD_RUM_ANDROID_APPLICATION_ID: ${{ secrets.DD_RUM_ANDROID_APPLICATION_ID }}
          DD_RUM_IOS_CLIENT_TOKEN: ${{ secrets.DD_RUM_IOS_CLIENT_TOKEN }}
          DD_RUM_IOS_APPLICATION_ID: ${{ secrets.DD_RUM_IOS_APPLICATION_ID }}
          DD_SITE: ${{ secrets.DD_SITE }}
        run: docker-compose build
```

**Required GitHub Secrets:**

- `DD_RUM_WEB_CLIENT_TOKEN`
- `DD_RUM_WEB_APPLICATION_ID`
- `DD_RUM_WEB_SERVICE`
- `DD_RUM_ANDROID_CLIENT_TOKEN`
- `DD_RUM_ANDROID_APPLICATION_ID`
- `DD_RUM_IOS_CLIENT_TOKEN`
- `DD_RUM_IOS_APPLICATION_ID`

### GitLab CI

```yaml
variables:
  DD_RUM_WEB_CLIENT_TOKEN: $DD_RUM_WEB_CLIENT_TOKEN
  DD_RUM_WEB_APPLICATION_ID: $DD_RUM_WEB_APPLICATION_ID
  DD_RUM_WEB_SERVICE: $DD_RUM_WEB_SERVICE
  DD_RUM_ANDROID_CLIENT_TOKEN: $DD_RUM_ANDROID_CLIENT_TOKEN
  DD_RUM_ANDROID_APPLICATION_ID: $DD_RUM_ANDROID_APPLICATION_ID
  DD_RUM_IOS_CLIENT_TOKEN: $DD_RUM_IOS_CLIENT_TOKEN
  DD_RUM_IOS_APPLICATION_ID: $DD_RUM_IOS_APPLICATION_ID
  DD_SITE: $DD_SITE

build:
  script:
    - docker-compose build
```

## Verification

### Web Dashboard

1. **Check build logs:**

   ```bash
   docker-compose build api 2>&1 | grep "RUM credentials"
   # Should show: ✅ RUM credentials injected into index.html
   ```

2. **Verify in container:**

   ```bash
   docker exec datadog-maui-api cat wwwroot/index.html | grep -A 5 "DD_RUM.init"
   # Should show actual credentials, not PLACEHOLDER_*
   ```

3. **Test in browser:**
   - Open http://localhost:5000
   - Open browser DevTools → Console
   - Look for Datadog RUM initialization messages
   - Check Network tab for requests to `datadoghq-browser-agent.com`

4. **Verify in Datadog:**
   - Go to [RUM Applications](https://app.datadoghq.com/rum/list)
   - Select `datadog-maui-web`
   - You should see sessions appearing

### Mobile Apps

1. **Check environment variables at runtime:**
   - Add debug logging in MauiProgram.cs:

   ```csharp
   Console.WriteLine($"RUM Client Token: {DatadogConfig.ClientToken}");
   Console.WriteLine($"RUM App ID: {DatadogConfig.RumApplicationId}");
   ```

2. **Run app and check logs:**

   ```bash
   # Android
   adb logcat | grep "RUM"

   # iOS
   # Check Xcode console output
   ```

3. **Verify in Datadog:**
   - Go to [RUM Applications](https://app.datadoghq.com/rum/list)
   - Select `datadog-maui-android` or `datadog-maui-ios`
   - You should see sessions appearing

## Troubleshooting

### Web: RUM not initializing

**Problem:** Browser console shows RUM errors or placeholder values visible

**Solutions:**

1. **Check environment variables are set:**

   ```bash
   echo $DD_RUM_WEB_CLIENT_TOKEN
   echo $DD_RUM_WEB_APPLICATION_ID
   ```

2. **Rebuild without cache:**

   ```bash
   docker-compose build --no-cache api
   ```

3. **Verify injection happened:**

   ```bash
   docker-compose build api 2>&1 | grep "RUM"
   ```

4. **Check source file has placeholders:**
   ```bash
   grep PLACEHOLDER Api/wwwroot/index.html
   # Should find PLACEHOLDER_CLIENT_TOKEN and PLACEHOLDER_APPLICATION_ID
   ```

### Mobile: Placeholder values appearing in logs

**Problem:** App logs show `PLACEHOLDER_ANDROID_CLIENT_TOKEN` or `PLACEHOLDER_IOS_CLIENT_TOKEN`

**Root Cause:** Environment variables weren't set during build, so no config file was generated.

**Solutions:**

1. **Export variables BEFORE building:**

   ```bash
   # Must set before dotnet build
   export DD_RUM_ANDROID_CLIENT_TOKEN="your_token_here"
   export DD_RUM_ANDROID_APPLICATION_ID="your_app_id_here"

   # Then build
   dotnet build -f net10.0-android
   ```

2. **Verify config file exists:**

   ```bash
   # Check if config file was generated
   ls -la MauiApp/Platforms/Android/datadog-rum.config

   # View contents
   cat MauiApp/Platforms/Android/datadog-rum.config
   ```

3. **Check build output:**

   Look for these messages during build:

   ```
   [MSBuild] 🐕 Generating Android RUM config file...
   [MSBuild]   Client Token: pub123...
   ```

   If you see this instead, credentials weren't found:

   ```
   [MSBuild] ⚠️  No Android RUM credentials found!
   ```

4. **Rebuild after setting variables:**

   ```bash
   # Clean first
   dotnet clean

   # Set environment variables
   export DD_RUM_ANDROID_CLIENT_TOKEN="your_token"
   export DD_RUM_ANDROID_APPLICATION_ID="your_app_id"

   # Rebuild
   dotnet build -f net10.0-android
   ```

5. **Alternative: Pass as MSBuild properties:**

   ```bash
   dotnet build -f net10.0-android \
     -p:AndroidClientToken="your_token" \
     -p:AndroidApplicationId="your_app_id"
   ```

### Credentials showing in logs

**Problem:** Sensitive credentials visible in build logs or runtime logs

**Solutions:**

1. **Never log credential values directly**
2. **Use masked variables in CI/CD**
3. **Review .gitignore includes .env**
4. **Add .env\* to .dockerignore**

## Security Best Practices

### 1. Never Commit Credentials

**✅ DO:**

- Use `.env` file (gitignored)
- Use CI/CD secrets
- Use environment variables

**❌ DON'T:**

- Hardcode credentials in source code
- Commit `.env` file
- Include credentials in comments or documentation

### 2. Use Separate Applications

Create separate RUM applications for each platform/environment:

- **Development**: `datadog-maui-web-dev`, `datadog-maui-android-dev`
- **Staging**: `datadog-maui-web-staging`
- **Production**: `datadog-maui-web-prod`

### 3. Rotate Credentials Regularly

- Regenerate client tokens periodically
- Update in all environments (dev, staging, prod)
- Revoke old tokens in Datadog UI

### 4. Limit Access

- Only grant RUM application access to necessary team members
- Use Datadog RBAC to control permissions
- Audit access logs regularly

## Mobile App Architecture Deep Dive

### How MSBuild Target Generation Works

The MAUI app uses MSBuild's extensibility to inject credentials at build time, solving several challenges:

**Challenges:**

- Mobile apps can't easily read `.env` files (file system restrictions)
- Environment variables aren't reliably available in mobile runtimes
- Hardcoded credentials in source code are insecure
- Need different credentials for Android vs iOS

**Solution:** Generate platform-specific config files during build and embed them as resources.

#### Step 1: Property Resolution (Build-Time)

**File:** [MauiApp/DatadogMauiApp.csproj](../../MauiApp/DatadogMauiApp.csproj) (lines 58-66)

```xml
<PropertyGroup>
  <!-- Read from environment if not already set -->
  <AndroidClientToken Condition="'$(AndroidClientToken)' == ''">$(DD_RUM_ANDROID_CLIENT_TOKEN)</AndroidClientToken>
  <AndroidApplicationId Condition="'$(AndroidApplicationId)' == ''">$(DD_RUM_ANDROID_APPLICATION_ID)</AndroidApplicationId>
</PropertyGroup>
```

MSBuild properties can come from:

- Command line: `dotnet build -p:AndroidClientToken=abc123`
- Environment variables: `DD_RUM_ANDROID_CLIENT_TOKEN`
- MSBuild property files

#### Step 2: Config File Generation (Build-Time)

**Android Target** (lines 69-83):

```xml
<Target Name="GenerateAndroidRumConfig" BeforeTargets="BeforeBuild" Condition="'$(TargetFramework)' == 'net10.0-android'">
  <WriteLinesToFile
    File="$(MSBuildProjectDirectory)/Platforms/Android/datadog-rum.config"
    Lines="DD_RUM_ANDROID_CLIENT_TOKEN=$(AndroidClientToken);DD_RUM_ANDROID_APPLICATION_ID=$(AndroidApplicationId)"
    Overwrite="true"
    Condition="'$(AndroidClientToken)' != ''" />
</Target>
```

**Key features:**

- Runs **before** compilation (`BeforeTargets="BeforeBuild"`)
- Only for Android (`Condition="'$(TargetFramework)' == 'net10.0-android'"`)
- Creates `Platforms/Android/datadog-rum.config` with `KEY=VALUE` format
- Shows build warnings if credentials are missing

**iOS Target** works identically for `Platforms/iOS/datadog-rum.config`

#### Step 3: Embed as Resource (Build-Time)

**Config File Embedding** (lines 101-109):

```xml
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-android'">
  <EmbeddedResource Include="Platforms/Android/datadog-rum.config" Condition="Exists('Platforms/Android/datadog-rum.config')" />
</ItemGroup>
```

The config file becomes an **embedded manifest resource** in the compiled assembly with the name:

- Android: `DatadogMauiApp.Platforms.Android.datadog-rum.config`
- iOS: `DatadogMauiApp.Platforms.iOS.datadog-rum.config`

#### Step 4: Load at Runtime

**File:** [MauiApp/Config/DatadogConfig.cs](../../MauiApp/Config/DatadogConfig.cs) (lines 20-71)

```csharp
private static Dictionary<string, string> LoadCredentials()
{
    var creds = new Dictionary<string, string>();

    // Platform-specific resource name (compile-time)
#if ANDROID
    var resourceName = "DatadogMauiApp.Platforms.Android.datadog-rum.config";
#elif IOS
    var resourceName = "DatadogMauiApp.Platforms.iOS.datadog-rum.config";
#endif

    // Load from embedded resource using reflection
    var assembly = Assembly.GetExecutingAssembly();
    using var stream = assembly.GetManifestResourceStream(resourceName);

    if (stream != null)
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        // Parse KEY=VALUE format
        foreach (var line in content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Trim().Split('=', 2);
            if (parts.Length == 2)
                creds[parts[0].Trim()] = parts[1].Trim();
        }
    }

    return creds;
}
```

**3-Tier Fallback** (lines 75-96):

```csharp
public static string ClientToken
{
    get
    {
#if ANDROID
        // Priority 1: Embedded config (build-time injected)
        var creds = LoadCredentials();
        if (creds.TryGetValue("DD_RUM_ANDROID_CLIENT_TOKEN", out var token))
            return token;

        // Priority 2: Runtime environment variable (for local dev)
        var envToken = Environment.GetEnvironmentVariable("DD_RUM_ANDROID_CLIENT_TOKEN");
        if (!string.IsNullOrEmpty(envToken))
            return envToken;

        // Priority 3: Placeholder (compile-time constant)
        return DefaultAndroidClientToken;
#endif
    }
}
```

### Why This Approach?

**Compared to environment variables only:**

- ✅ Works in sandboxed mobile environments
- ✅ No runtime environment variable support needed
- ✅ Credentials embedded in binary (secure, can't be modified)
- ✅ Platform-specific at compile time (no runtime branching)

**Compared to hardcoded credentials:**

- ✅ No credentials in source code
- ✅ Different values per environment (dev/staging/prod)
- ✅ CI/CD pipeline integration

**Compared to config files shipped with app:**

- ✅ No file system permissions needed
- ✅ Can't be modified after compilation
- ✅ Embedded resources always available
- ✅ Platform-specific paths automatic

### Build Flow Diagram

```
Environment Variables           MSBuild Properties
DD_RUM_ANDROID_CLIENT_TOKEN    -p:AndroidClientToken=...
        |                               |
        └──────────> .csproj <──────────┘
                       |
                       ├─> GenerateAndroidRumConfig target
                       |   └─> Platforms/Android/datadog-rum.config
                       |
                       ├─> Compile (csc)
                       |   └─> DatadogMauiApp.dll
                       |       └─> Embedded Resource:
                       |           DatadogMauiApp.Platforms.Android.datadog-rum.config
                       |
                       └─> Runtime
                           └─> DatadogConfig.LoadCredentials()
                               └─> GetManifestResourceStream()
                                   └─> Parse KEY=VALUE
                                       └─> Return credentials
```

## Additional Resources

- [Datadog RUM Browser Setup](https://docs.datadoghq.com/real_user_monitoring/browser/)
- [Datadog RUM Android Setup](https://docs.datadoghq.com/real_user_monitoring/mobile_and_tv_monitoring/android/)
- [Datadog RUM iOS Setup](https://docs.datadoghq.com/real_user_monitoring/mobile_and_tv_monitoring/ios/)
- [RUM Data Security](https://docs.datadoghq.com/real_user_monitoring/session_replay/privacy_options/)
- [MSBuild Custom Targets](https://learn.microsoft.com/en-us/visualstudio/msbuild/msbuild-targets)
- [.NET Assembly Embedded Resources](https://learn.microsoft.com/en-us/dotnet/api/system.reflection.assembly.getmanifestresourcestream)

---

**Status:** ✅ Configured

**Last Updated:** 2025-12-31

**Platforms:**

- ✅ Web (index.html with build-time injection)
- ✅ Android (runtime environment variables)
- ✅ iOS (runtime environment variables)

**Security:**

- ✅ Credentials in .env (gitignored)
- ✅ .env.example template provided
- ✅ Placeholder values in source code
- ✅ Build-time injection for web
- ✅ Runtime environment variables for mobile
