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
      site: "datadoghq.com",
      service: "datadog-maui-web",
      env: "local",
      version: "1.0.0",
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

**File:** [MauiApp/Config/DatadogConfig.cs](MauiApp/Config/DatadogConfig.cs)

Mobile apps read RUM credentials from **environment variables at runtime** with fallback to placeholders.

```csharp
public static class DatadogConfig
{
    // Fallback defaults (placeholders)
    private const string DefaultAndroidClientToken = "PLACEHOLDER_ANDROID_CLIENT_TOKEN";
    private const string DefaultAndroidRumApplicationId = "PLACEHOLDER_ANDROID_APPLICATION_ID";
    private const string DefaultIosClientToken = "PLACEHOLDER_IOS_CLIENT_TOKEN";
    private const string DefaultIosRumApplicationId = "PLACEHOLDER_IOS_APPLICATION_ID";

    // Get platform-specific client token
    // Priority: Environment Variable > Default Value
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
#else
            return string.Empty;
#endif
        }
    }

    // Similar pattern for RumApplicationId...
}
```

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

```bash
# Export environment variables before building
export DD_RUM_ANDROID_CLIENT_TOKEN="pub699a12..."
export DD_RUM_ANDROID_APPLICATION_ID="7658abf2..."

# Build Android app
dotnet build -f net10.0-android
```

#### iOS

```bash
# Export environment variables before building
export DD_RUM_IOS_CLIENT_TOKEN="pub9b087a..."
export DD_RUM_IOS_APPLICATION_ID="6de15c6c..."

# Build iOS app
dotnet build -f net10.0-ios
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
          DD_RUM_ANDROID_CLIENT_TOKEN: ${{ secrets.DD_RUM_ANDROID_CLIENT_TOKEN }}
          DD_RUM_ANDROID_APPLICATION_ID: ${{ secrets.DD_RUM_ANDROID_APPLICATION_ID }}
          DD_RUM_IOS_CLIENT_TOKEN: ${{ secrets.DD_RUM_IOS_CLIENT_TOKEN }}
          DD_RUM_IOS_APPLICATION_ID: ${{ secrets.DD_RUM_IOS_APPLICATION_ID }}
        run: docker-compose build
```

**Required GitHub Secrets:**

- `DD_RUM_WEB_CLIENT_TOKEN`
- `DD_RUM_WEB_APPLICATION_ID`
- `DD_RUM_ANDROID_CLIENT_TOKEN`
- `DD_RUM_ANDROID_APPLICATION_ID`
- `DD_RUM_IOS_CLIENT_TOKEN`
- `DD_RUM_IOS_APPLICATION_ID`

### GitLab CI

```yaml
variables:
  DD_RUM_WEB_CLIENT_TOKEN: $DD_RUM_WEB_CLIENT_TOKEN
  DD_RUM_WEB_APPLICATION_ID: $DD_RUM_WEB_APPLICATION_ID
  DD_RUM_ANDROID_CLIENT_TOKEN: $DD_RUM_ANDROID_CLIENT_TOKEN
  DD_RUM_ANDROID_APPLICATION_ID: $DD_RUM_ANDROID_APPLICATION_ID
  DD_RUM_IOS_CLIENT_TOKEN: $DD_RUM_IOS_CLIENT_TOKEN
  DD_RUM_IOS_APPLICATION_ID: $DD_RUM_IOS_APPLICATION_ID

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

### Mobile: Environment variables not being read

**Problem:** App uses placeholder values instead of environment variables

**Solutions:**

1. **Export variables before building:**

   ```bash
   export DD_RUM_ANDROID_CLIENT_TOKEN="your_token_here"
   export DD_RUM_ANDROID_APPLICATION_ID="your_app_id_here"
   ```

2. **Set variables in IDE:**

   - **Visual Studio**: Project Properties → Debug → Environment Variables
   - **VS Code**: Add to launch.json
   - **Rider**: Run Configurations → Environment Variables

3. **Alternative: Use configuration file (not recommended for production):**
   - Create a local config file (gitignored)
   - Read values from file in DatadogConfig.cs
   - Only for local development

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

## Additional Resources

- [Datadog RUM Browser Setup](https://docs.datadoghq.com/real_user_monitoring/browser/)
- [Datadog RUM Android Setup](https://docs.datadoghq.com/real_user_monitoring/mobile_and_tv_monitoring/android/)
- [Datadog RUM iOS Setup](https://docs.datadoghq.com/real_user_monitoring/mobile_and_tv_monitoring/ios/)
- [RUM Data Security](https://docs.datadoghq.com/real_user_monitoring/session_replay/privacy_options/)

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
