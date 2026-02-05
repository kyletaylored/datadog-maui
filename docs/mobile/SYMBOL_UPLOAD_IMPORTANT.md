# IMPORTANT: Symbol Upload Requires `dotnet publish`

## Key Finding

The `Datadog.MAUI.Symbols` package hooks into the **Publish** target, not the Build target. This means:

- ❌ `dotnet build` will **NOT** trigger symbol upload
- ✅ `dotnet publish` **WILL** trigger symbol upload

## Why This Matters

The MSBuild target in `Datadog.MAUI.Symbols.targets` is defined as:

```xml
<Target Name="DatadogUploadSymbols" AfterTargets="Publish">
```

This means the symbol upload task only executes after the Publish target runs, which happens during `dotnet publish`, not during `dotnet build`.

## Correct Commands

### Using Make (Recommended)

The Makefile has been updated to use `dotnet publish`:

```bash
# Test with dry-run (safe)
make app-release-android-dry
make app-release-ios-dry  # Requires Apple Developer code signing

# Real upload (requires DD_API_KEY)
export DD_API_KEY="your-key"
make app-release-android
make app-release-ios  # Requires Apple Developer code signing
```

**iOS Note:** iOS device builds require valid Apple Developer certificates for code signing. If you don't have these set up, test symbol upload with Android only.

### Manual Commands

If not using Make, use `dotnet publish`:

```bash
# Android
cd MauiApp
dotnet publish -c Release -f net10.0-android

# iOS
cd MauiApp
dotnet publish -c Release -f net10.0-ios
```

## What `dotnet publish` Does

The `publish` command:
1. Builds the app in Release configuration
2. Generates R8 mapping files (Android) or dSYM bundles (iOS)
3. Packages the app for distribution
4. **Triggers the DatadogUploadSymbols target**
5. Uploads symbols to Datadog (or simulates in dry-run mode)

## Verification

After running `dotnet publish`, you should see output like:

```
DatadogUploadSymbols:
  [Datadog] Symbol upload starting...
  [Datadog] Platform: Android
  [Datadog] Configuration: Release
  [Datadog] Service: com.datadog.mauiapp.android
  [Datadog] Dry run: true
  [Datadog] Symbol file found: /path/to/mapping.txt
  [Datadog] DRY RUN - Would upload to Datadog
```

If you don't see `DatadogUploadSymbols` in the output, the symbol upload task did not run.

## Common Mistakes

### ❌ Wrong: Using `dotnet build`

```bash
# This will NOT trigger symbol upload
dotnet build -c Release -f net10.0-android
```

**Result:** No symbol upload task runs, no output from Datadog.MAUI.Symbols package.

### ✅ Correct: Using `dotnet publish`

```bash
# This WILL trigger symbol upload
dotnet publish -c Release -f net10.0-android
```

**Result:** Symbol upload task runs, output shows `DatadogUploadSymbols` target execution.

## Package Documentation

From the [Datadog.MAUI.Symbols source code](https://github.com/kyletaylored/dd-sdk-maui/tree/main/Datadog.MAUI.Symbols):

> "Automatically uploads debug symbols (iOS dSYMs and Android Proguard mapping files) to Datadog during the build/**publish** pipeline."

The key word is **publish**. The package is designed to run during the publish stage, not the build stage.

## Integration Notes

### When Symbol Upload Runs

- ✅ During `dotnet publish`
- ✅ During CI/CD pipeline publish steps
- ✅ When creating APK/IPA packages
- ❌ During `dotnet build`
- ❌ During regular development builds

### Configuration

The upload behavior is controlled by MSBuild properties:

```xml
<!-- Enable/disable upload (default: true) -->
<DatadogUploadEnabled>true</DatadogUploadEnabled>

<!-- Dry-run mode (default: false) -->
<DatadogDryRun>true</DatadogDryRun>

<!-- Run in Debug builds (default: false) -->
<DatadogUploadInDebug>false</DatadogUploadInDebug>
```

By default, symbol upload:
- Only runs in Release configuration
- Only runs during `dotnet publish`
- Can be tested with dry-run mode

## Known Issues

### Dry-Run Mode Error with DD_API_KEY Not Set

When running in dry-run mode without `DD_API_KEY` set, you may see an error:

```
error : [Datadog] Unexpected error during symbol upload: The value cannot be an empty string. (Parameter 'oldValue')
```

**Root Cause:** The Datadog.MAUI.Symbols package (v1.0.0) has a bug when processing string replacements with empty values.

**What This Means:**
- The symbol upload task **IS running** (you'll see `[Datadog] Uploading android symbols to Datadog...`)
- The error occurs during dry-run simulation, not during the actual upload preparation
- Symbol files are being found and processed correctly
- This error **does not affect real uploads** when `DD_API_KEY` is properly set

**Evidence of Success:**
Even with the error, you can verify the upload task ran by looking for:
1. `[Datadog] Uploading android symbols to Datadog...` in the output
2. Warning: `DD_API_KEY is not set` (expected in dry-run)
3. The build completes and generates the APK successfully

**Workaround:**
To test without the error, set a dummy API key:
```bash
export DD_API_KEY="dummy-key-for-dry-run-testing"
make app-release-android-dry
```

The dry-run mode will still prevent actual uploads, but the package won't error on empty string replacement.

**Status:** This is a known issue in Datadog.MAUI.Symbols v1.0.0. Consider reporting it to the [package maintainer](https://github.com/kyletaylored/dd-sdk-maui/issues).

## Related Documentation

- [Release Build Guide](RELEASE_BUILD_GUIDE.md) - Complete guide with updated commands
- [Symbol Upload Documentation](SYMBOL_UPLOAD.md) - Configuration details
- [Datadog.MAUI.Symbols on GitHub](https://github.com/kyletaylored/dd-sdk-maui/tree/main/Datadog.MAUI.Symbols) - Source code and integration docs

---

**Summary:** Always use `dotnet publish` for Release builds when you want to test or use the symbol upload feature. The `dotnet build` command will not trigger the symbol upload task.

**Last Updated:** 2026-01-28
