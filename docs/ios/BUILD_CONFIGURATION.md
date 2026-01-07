# iOS Build Notes

## Current Status

✅ **iOS builds are now WORKING!** The solution was to force iOS SDK 26.0 (compatible with Xcode 26.0).

✅ **Android builds work perfectly** with all Datadog integrations (RUM, APM, distributed tracing, logging).

## The Problem

**Environment:**
- Xcode version: **26.0** (downgraded from 26.1.1)
- .NET SDK: 10.0.101
- .NET MAUI workload: 10.0.1
- iOS SDK (in workload): 26.2.10191

**Issue:**
The .NET 10 MAUI workload (version 10.0.1) includes iOS SDK 26.2, which requires Xcode 26.2. When using Xcode 26.0:

1. ✅ **Compilation succeeds** - C# code compiles fine
2. ✅ **Xcode version check bypassed** - Using `_IsMatchingXcode=true` property
3. ✅ **IL Linker bypassed** - Using `RunILLink=false` property
4. ❌ **Native linker fails** - The native linker (clang++) cannot find symbols because iOS SDK 26.2 requires Xcode 26.2's headers and libraries

**Error:**
```
error : clang++ exited with code 1:
error : Undefined symbols for architecture arm64:
error :   "_main", referenced from:
error :       <initial-undefines>
error : ld: symbol(s) not found for architecture arm64
error : clang++: error: linker command failed with exit code 1 (use -v to see invocation)
```

## Why We Can't Upgrade Xcode

- **Xcode 26.1**: No .NET support
- **Xcode 26.2**: Breaks .NET 8 compatibility, which is required for this project
- **Xcode 26.0**: Currently installed, but iOS SDK 26.2 (bundled with MAUI 10.0.1) expects Xcode 26.2

## The Solution ✅

Both iOS SDK 26.0 and 26.2 are installed with the MAUI workload:
```
Microsoft.iOS.Sdk.net10.0_26.0 version 26.0.11017
Microsoft.iOS.Ref.net10.0_26.2 version 26.2.10191
```

The fix is to force the build to use iOS SDK 26.0 (compatible with Xcode 26.0) instead of defaulting to 26.2.

### Configuration in `DatadogMauiApp.csproj`:

```xml
<!-- Force iOS SDK 26.0 (compatible with Xcode 26.0) instead of 26.2 -->
<RuntimeIdentifier Condition="'$(TargetFramework)' == 'net10.0-ios'">iossimulator-arm64</RuntimeIdentifier>
<TargetPlatformVersion Condition="'$(TargetFramework)' == 'net10.0-ios'">26.0</TargetPlatformVersion>

<!-- Bypass Xcode version check for iOS (Xcode 26.0 with .NET iOS SDK 26.0) -->
<_IsMatchingXcode Condition="'$(TargetFramework)' == 'net10.0-ios'">true</_IsMatchingXcode>

<!-- Disable code signing for iOS simulator builds -->
<CodesignKey Condition="'$(TargetFramework)' == 'net10.0-ios'"></CodesignKey>
<CodesignProvision Condition="'$(TargetFramework)' == 'net10.0-ios'"></CodesignProvision>
<CodesignEntitlements Condition="'$(TargetFramework)' == 'net10.0-ios'"></CodesignEntitlements>

<!-- iOS build configuration to work with Xcode 26.0 and .NET iOS SDK 26.0 -->
<!-- Use standard AOT compilation (no interpreter mode) -->
<MtouchLink Condition="'$(TargetFramework)' == 'net10.0-ios'">None</MtouchLink>
```

### Key Properties Explained:

1. **`TargetPlatformVersion=26.0`**: Forces use of iOS SDK 26.0 pack instead of 26.2
2. **`RuntimeIdentifier=iossimulator-arm64`**: Targets iOS simulator on Apple Silicon
3. **`_IsMatchingXcode=true`**: Bypasses Xcode version compatibility check
4. **`CodesignKey/CodesignProvision/CodesignEntitlements` (empty)**: Disables code signing for simulator
5. **`MtouchLink=None`**: No managed code linking (avoids SDK version issues)
6. **`MtouchRegistrar=Dynamic`**: Uses dynamic registrar for Debug builds (avoids runtime hash mismatches between SDK versions)

## Build Commands

### Build iOS for Simulator:
```bash
cd MauiApp
dotnet build -f net10.0-ios
```

### Run iOS on Simulator:
```bash
# List available simulators
xcrun simctl list devices available | grep -i iphone

# Run on a specific simulator (example: iPhone 16 Pro)
dotnet build -t:Run -f net10.0-ios
```

## Solutions Attempted (Before Finding The Fix)

### ❌ 1. Bypass Xcode Version Check Only
**Tried:** Setting `_IsMatchingXcode=true` in csproj
**Result:** Compilation succeeds but native linker fails (SDK 26.2 incompatible with Xcode 26.0)

### ❌ 2. Disable Managed Linker
**Tried:** Setting `MtouchLink=None`, `ILLinkEnabled=false`
**Result:** Native linker still uses SDK 26.2 and fails

### ❌ 3. Directory.Build.props Overrides
**Tried:** Various MSBuild properties in Directory.Build.props
**Result:** Workload resolver ignores overrides and uses SDK 26.2

### ❌ 4. Interpreter Mode
**Tried:** `UseInterpreter=true`, `MtouchInterpreter=-all`
**Result:** Still uses SDK 26.2, same linker error

### ✅ 5. Force TargetPlatformVersion=26.0 (WORKING!)
**Tried:** Setting `TargetPlatformVersion=26.0` to use iOS SDK 26.0 pack
**Result:** ✅ Build succeeds! Uses iOS SDK 26.0 which is compatible with Xcode 26.0

## Alternative Solutions (No Longer Needed)

### Option 1: Use Android Only

**Note**: This is no longer needed - iOS build is now working!

Focus development on Android which has full Datadog integration working:
- ✅ RUM (Real User Monitoring)
- ✅ APM (Application Performance Monitoring)
- ✅ Distributed Tracing (Mobile → API)
- ✅ Logging with ILogger
- ✅ Session Replay
- ✅ Crash Reporting

### Option 2: Downgrade MAUI Workload

**Note**: This is no longer needed - iOS build is now working!

If you have admin/sudo access:

```bash
# Uninstall current MAUI workload
sudo /usr/local/share/dotnet/dotnet workload uninstall maui

# Install older version with iOS SDK 26.0
sudo /usr/local/share/dotnet/dotnet workload install maui --version 10.0.0

# Verify
/usr/local/share/dotnet/dotnet workload list
```

This would install MAUI 10.0.0 which includes iOS SDK 26.0 (compatible with Xcode 26.0-26.1.x).

### Option 3: Use .NET 9 for iOS (Not Recommended)

Temporarily switch iOS target to `net9.0-ios`:

```xml
<!-- In DatadogMauiApp.csproj -->
<TargetFrameworks>net10.0-android;net9.0-ios</TargetFrameworks>
```

**Pros:** .NET 9 iOS SDK is compatible with Xcode 26.1.1
**Cons:** Mixed .NET versions, iOS features behind Android

### Option 4: Wait for Xcode 26.1.x Support

Monitor .NET MAUI releases for an update that adds Xcode 26.1.x compatibility to iOS SDK 26.2.

Check: https://github.com/dotnet/maui/issues

## Project Configuration

The following properties have been added to support iOS builds when SDK mismatch is resolved:

**In `DatadogMauiApp.csproj`:**
```xml
<!-- Bypass Xcode version check -->
<_IsMatchingXcode Condition="'$(TargetFramework)' == 'net10.0-ios'">true</_IsMatchingXcode>

<!-- Disable managed linker to avoid SDK issues -->
<MtouchLink Condition="'$(TargetFramework)' == 'net10.0-ios'">None</MtouchLink>
<ILLinkEnabled Condition="'$(TargetFramework)' == 'net10.0-ios'">false</ILLinkEnabled>
```

**In `Platforms/iOS/AppDelegate.cs`:**
- Datadog initialization is currently disabled (commented out)
- iOS namespace imports are commented out pending working build
- Ready to be enabled once build issues are resolved

## iOS Datadog Integration Status

The iOS Datadog SDK bindings are installed and ready:

```xml
<PackageReference Include="Bcr.Datadog.iOS.Core" Version="2.26.0" />
<PackageReference Include="Bcr.Datadog.iOS.Logs" Version="2.26.0" />
<PackageReference Include="Bcr.Datadog.iOS.RUM" Version="2.26.0" />
<PackageReference Include="Bcr.Datadog.iOS.Trace" Version="2.26.0" />
<PackageReference Include="Bcr.Datadog.iOS.ObjC" Version="2.26.0" />
```

Once the build works, iOS will have the same Datadog capabilities as Android.

## Testing the Workaround

If you want to verify the current workarounds work up until the linker phase:

```bash
# Should compile successfully and fail at link phase
cd MauiApp
dotnet build -f net10.0-ios -v normal 2>&1 | grep -A5 -B5 "ILLINK"
```

## Recommendation

✅ **Both platforms are now working!**

- **iOS**: Builds successfully for iOS Simulator using SDK 26.0 (compatible with Xcode 26.0)
- **Android**: Fully functional with all Datadog integrations (RUM, APM, distributed tracing, logging, Session Replay, Crash Reporting)

### Next Steps for iOS

1. **Test the iOS build on simulator**:
   ```bash
   cd MauiApp
   dotnet build -t:Run -f net10.0-ios
   ```

2. **Enable Datadog initialization** in [AppDelegate.cs](Platforms/iOS/AppDelegate.cs) once you verify the app runs

3. **Add iOS RUM credentials** to `.env` file:
   ```bash
   DD_RUM_IOS_CLIENT_TOKEN=your_ios_token
   DD_RUM_IOS_APPLICATION_ID=your_ios_app_id
   DATADOG_API_KEY=your_api_key  # For dSYM upload
   ```

4. **For physical device testing**, you'll need to:
   - Change `RuntimeIdentifier` from `iossimulator-arm64` to `ios-arm64`
   - Add proper code signing configuration (developer certificate and provisioning profile)

5. **Configure crash reporting** (optional but recommended):
   - See [iOS dSYM Crash Reporting Guide](IOS_DSYM_CRASH_REPORTING.md) for detailed setup
   - dSYM files are automatically generated for Release builds
   - Use `./upload-dsyms.sh` script to upload dSYMs to Datadog

The codebase is structured to support both platforms with identical Datadog capabilities!
