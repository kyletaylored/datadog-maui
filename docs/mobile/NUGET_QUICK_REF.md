# NuGet Quick Reference

Quick commands for working with Datadog MAUI NuGet feeds.

## Package Search

```bash
# Search all sources
dotnet package search "Datadog"

# Search specific source
dotnet package search "Datadog" --source DatadogMAUI --prerelease

# Search with more results
dotnet package search "Datadog" --source DatadogMAUI --take 50

# JSON output
dotnet package search "Datadog" --source DatadogMAUI --format json
```

## Source Management

```bash
# List configured sources
dotnet nuget list source

# Add source
dotnet nuget add source "https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json" --name "DatadogMAUI"

# Remove source
dotnet nuget remove source "DatadogMAUI"

# Update source
dotnet nuget update source "DatadogMAUI" --source "https://new-url.com"

# Disable source
dotnet nuget disable source "DatadogMAUI"

# Enable source
dotnet nuget enable source "DatadogMAUI"
```

## Package Installation

```bash
# Install from default source
dotnet add package Datadog.MAUI --version 3.5.0

# Install from specific source
dotnet add package Datadog.MAUI --version 3.5.0 --source DatadogMAUI

# Install prerelease
dotnet add package Datadog.MAUI --prerelease --source DatadogMAUI
```

## Package Management

```bash
# List installed packages
dotnet list package

# List with transitive dependencies
dotnet list package --include-transitive

# Filter by pattern
dotnet list package --include-transitive | grep Datadog

# Show outdated packages
dotnet list package --outdated

# Remove package
dotnet remove package Datadog.MAUI
```

## Restore & Cache

```bash
# Restore packages
dotnet restore

# Restore with no cache
dotnet restore --no-cache

# Restore with specific source
dotnet restore --source DatadogMAUI

# Clear all caches
dotnet nuget locals all --clear

# Clear specific cache
dotnet nuget locals http-cache --clear
dotnet nuget locals global-packages --clear
dotnet nuget locals temp --clear

# List cache locations
dotnet nuget locals all --list
```

## Build with Package Sources

```bash
# Build (uses NuGet.config automatically)
dotnet build

# Build with no restore (faster)
dotnet build --no-restore

# Build with force restore
dotnet build --force
```

## Troubleshooting Commands

```bash
# Verbose restore output
dotnet restore --verbosity detailed

# Check what sources are being used
dotnet restore --verbosity normal | grep -i source

# Test feed accessibility
curl -I "https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json"

# Verify NuGet.config
cat NuGet.config

# Check project package references
grep PackageReference MauiApp/DatadogMauiApp.csproj
```

## CI/CD Examples

### GitHub Actions

```yaml
- name: Restore packages
  run: dotnet restore
  env:
    NUGET_AUTH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### Azure DevOps

```yaml
- task: DotNetCoreCLI@2
  displayName: 'Restore NuGet packages'
  inputs:
    command: 'restore'
    feedsToUse: 'config'
    nugetConfigPath: 'NuGet.config'
```

## Current Project Configuration

### Package Sources
- **nuget.org**: `https://api.nuget.org/v3/index.json`
- **DatadogMAUI**: `https://kyletaylored.github.io/dd-sdk-maui/nuget/index.json`
- **Local**: `./local-packages`

### Installed Datadog Packages
- Datadog.MAUI.Symbols (1.0.0) - from local
- Bcr.Datadog.Android.Sdk.* (2.21.0-pre.1) - from nuget.org
- Bcr.Datadog.iOS.* (2.26.0) - from nuget.org

### Available from DatadogMAUI Feed
- Datadog.MAUI (3.5.0)
- 26 platform-specific packages (see NUGET_FEEDS.md)
