# Diagnostics Page

The Diagnostics page provides a comprehensive view of the Datadog SDK configuration and runtime values in the MAUI application.

## Overview

The diagnostics page displays all relevant Datadog configuration values including:
- Platform information (OS, app version, framework)
- Core SDK configuration (service name, environment, site)
- RUM configuration (application ID, client token, sample rates)
- Credential source information
- SDK version details

## Features

### Platform Information
- **Platform**: Current operating system (Android, iOS, Mac Catalyst, Windows)
- **App Version**: Application version and build number
- **Framework**: Target framework version

### Core Configuration
- **Service Name**: Datadog service identifier
- **Environment**: Deployment environment (local, dev, staging, production)
- **Site**: Datadog site region (US1, EU1, etc.)
- **Verbose Logging**: Whether debug logging is enabled

### RUM Configuration
- **Application ID**: RUM application identifier (masked for security)
- **Client Token**: RUM client token (masked for security)
- **Session Sample Rate**: Percentage of sessions tracked (0-100%)
- **Session Replay Rate**: Percentage of sessions recorded (0-100%)

### Credential Source
The page indicates where credentials are loaded from:
- ✅ **Embedded config file**: Generated at build time from MSBuild properties
- ✅ **Environment variables**: Set via `DD_RUM_*_CLIENT_TOKEN` and `DD_RUM_*_APPLICATION_ID`
- ⚠️ **Default placeholders**: No credentials configured (placeholder values used)

Priority order:
1. Embedded config file (generated at build time)
2. Environment variables
3. Default placeholder values

### Security Features

**Sensitive Value Masking**: Client tokens and application IDs are automatically masked:
- Shows first 8 and last 4 characters
- Example: `pub12345...abcd` instead of full token
- Placeholder values are clearly marked with ⚠️

## Usage

### Accessing the Diagnostics Page
1. Launch the MAUI app
2. Navigate to the **Diagnostics** tab in the bottom navigation
3. View all configuration values

### Refreshing Configuration
Click the **"Refresh Configuration"** button to reload all values and update the timestamp.

## Implementation Details

### Files
- **DiagnosticsPage.xaml**: UI layout with color-coded sections
- **DiagnosticsPage.xaml.cs**: Code-behind with configuration loading logic
- **AppShell.xaml**: Navigation shell with diagnostics tab

### Color-Coded Sections
Each configuration section uses a distinct color scheme for easy identification:
- **Platform Info**: Blue (`#E3F2FD`)
- **Core Config**: Purple (`#F3E5F5`)
- **RUM Config**: Green (`#E8F5E9`)
- **Credential Source**: Orange (`#FFF3E0`)
- **SDK Info**: Pink (`#FCE4EC`)

### Data Source
All configuration values are read from the `DatadogConfig` static class:
- `DatadogConfig.ServiceName`
- `DatadogConfig.Environment`
- `DatadogConfig.ClientToken`
- `DatadogConfig.RumApplicationId`
- `DatadogConfig.SessionSampleRate`
- `DatadogConfig.SessionReplaySampleRate`
- `DatadogConfig.Site`
- `DatadogConfig.VerboseLogging`

## Use Cases

### Development
- Verify credentials are loaded correctly
- Check environment configuration before testing
- Troubleshoot RUM initialization issues
- Confirm sample rates are set as expected

### Debugging
- Identify credential source (embedded vs environment)
- Verify platform-specific configuration
- Check if placeholder values are being used
- Confirm SDK version and package source

### Demo/Training
- Show configuration values without exposing full credentials
- Explain credential loading priority
- Demonstrate platform-specific differences
- Validate configuration in different environments

## Configuration Priority

The diagnostics page helps verify the credential loading priority:

```
1. Embedded Config File (highest priority)
   ↓
2. Environment Variables
   ↓
3. Default Placeholders (lowest priority)
```

### Embedded Config File
Generated at build time via MSBuild targets:
- Android: `Platforms/Android/datadog-rum.config`
- iOS: `Platforms/iOS/datadog-rum.config`

Set via MSBuild properties or environment variables before build:
```bash
export DD_RUM_ANDROID_CLIENT_TOKEN="your-token"
export DD_RUM_ANDROID_APPLICATION_ID="your-app-id"
dotnet build
```

### Environment Variables
Runtime environment variables (checked if embedded config not found):
- Android: `DD_RUM_ANDROID_CLIENT_TOKEN`, `DD_RUM_ANDROID_APPLICATION_ID`
- iOS: `DD_RUM_IOS_CLIENT_TOKEN`, `DD_RUM_IOS_APPLICATION_ID`

### Default Placeholders
Fallback values when no credentials found:
- `PLACEHOLDER_ANDROID_CLIENT_TOKEN`
- `PLACEHOLDER_ANDROID_APPLICATION_ID`
- `PLACEHOLDER_IOS_CLIENT_TOKEN`
- `PLACEHOLDER_IOS_APPLICATION_ID`

## Screenshots

The diagnostics page provides a clear, organized view of all Datadog configuration values with:
- Color-coded sections for easy scanning
- Masked sensitive values for security
- Clear indication of credential source
- Refresh capability for real-time updates
- Timestamp showing when values were last loaded

## Related Documentation

- [Datadog Configuration](RUM_CONFIGURATION.md) - How dynamic credential loading works
- [Symbol Upload](SYMBOL_UPLOAD.md) - Symbol upload configuration
- [MAUI Migration](DATADOG_MAUI_MIGRATION.md) - Migration to Datadog.MAUI v3.5.0

---

**Added:** 2026-01-28
**Location:** `MauiApp/Pages/DiagnosticsPage.xaml` and `.xaml.cs`
**Tab Icon:** Settings icon (shared with API Test tab)
