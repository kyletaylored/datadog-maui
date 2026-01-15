# iOS SDK Version Fix - Critical Information

## TL;DR

**Problem**: iOS builds fail with `Undefined symbols: _main` error when using Xcode 26.0.

**Root Cause**: .NET MAUI workload 10.0.1 includes both iOS SDK 26.0 and 26.2, but defaults to 26.2 which requires Xcode 26.2.

**Solution**: Force iOS SDK 26.0 by setting `TargetPlatformVersion=26.0` in the csproj.

## The Discovery

During iOS build troubleshooting, we discovered that the MAUI workload includes multiple iOS SDK versions:

```bash
$ dotnet workload list
Microsoft.iOS.Sdk.net10.0_26.0 version 26.0.11017   ← Compatible with Xcode 26.0
Microsoft.iOS.Ref.net10.0_26.2 version 26.2.10191   ← Default, requires Xcode 26.2
```

The build system defaults to using SDK 26.2, which causes native linker failures when Xcode 26.0 is installed because SDK 26.2 expects headers and libraries from Xcode 26.2.

## The Configuration

Add these properties to your iOS project configuration:

```xml
<!-- Force iOS SDK 26.0 (compatible with Xcode 26.0) instead of 26.2 -->
<RuntimeIdentifier Condition="'$(TargetFramework)' == 'net10.0-ios'">iossimulator-arm64</RuntimeIdentifier>
<TargetPlatformVersion Condition="'$(TargetFramework)' == 'net10.0-ios'">26.0</TargetPlatformVersion>

<!-- Bypass Xcode version check since we're using the correct SDK version -->
<_IsMatchingXcode Condition="'$(TargetFramework)' == 'net10.0-ios'">true</_IsMatchingXcode>

<!-- Disable code signing for simulator development -->
<CodesignKey Condition="'$(TargetFramework)' == 'net10.0-ios'"></CodesignKey>
<CodesignProvision Condition="'$(TargetFramework)' == 'net10.0-ios'"></CodesignProvision>
<CodesignEntitlements Condition="'$(TargetFramework)' == 'net10.0-ios'"></CodesignEntitlements>

<!-- Disable managed code linking to avoid any SDK compatibility issues -->
<MtouchLink Condition="'$(TargetFramework)' == 'net10.0-ios'">None</MtouchLink>
```

## Key Property Explained

**`TargetPlatformVersion=26.0`** is the critical property. This tells MSBuild to use the iOS SDK 26.0 pack instead of defaulting to 26.2.

Without this, the build process will:
1. ✅ Compile C# code successfully
2. ✅ Run IL Linker (if enabled)
3. ❌ Fail during native linking with `Undefined symbols: _main`

With this property:
1. ✅ Compile C# code successfully
2. ✅ Run IL Linker (if enabled)
3. ✅ Successfully link native code using Xcode 26.0's toolchain

## Verification

To verify the fix is working, check the build output for the SDK path:

```bash
dotnet build -f net10.0-ios -v minimal

# Look for this path in the output:
# /usr/local/share/dotnet/packs/Microsoft.iOS.Sdk.net10.0_26.0/26.0.11017/...
#                                                         ^^^^^^
#                                                    Should be 26.0, not 26.2
```

## Environment Constraints

This configuration is necessary when:
- Using Xcode 26.0 or 26.1
- Using .NET MAUI workload 10.0.1
- Cannot upgrade to Xcode 26.2 (due to .NET 8 compatibility issues)
- Cannot downgrade MAUI workload (requires sudo access)

## Alternative Solutions

If you CAN upgrade/downgrade:

1. **Upgrade Xcode to 26.2** - Allows using default SDK 26.2 (may break .NET 8 projects)
2. **Downgrade MAUI workload to 10.0.0** - Uses only SDK 26.0 (requires sudo)
3. **Use .NET 9 for iOS** - Target `net9.0-ios` instead (older SDK version)

## Applicability

This fix applies to any .NET MAUI project targeting iOS with:
- .NET 10
- MAUI workload 10.0.1
- Xcode 26.0 or 26.1

The issue does **not** affect:
- Android builds (always work)
- iOS builds with Xcode 26.2
- .NET 9 or earlier iOS projects

## References

- Full documentation: [iOS_BUILD_NOTES.md](iOS_BUILD_NOTES.md)
- Project configuration: [DatadogMauiApp.csproj](DatadogMauiApp.csproj#L25-L40)
- Build configuration: [Directory.Build.props](Directory.Build.props)

## Success Metrics

After applying this fix:
- ✅ iOS build completes successfully
- ✅ `.app` bundle created in `bin/Debug/net10.0-ios/iossimulator-arm64/`
- ✅ App runs on iOS Simulator
- ✅ No native linker errors
- ✅ Build uses SDK 26.0 path in output

## Credits

This solution was discovered through collaborative troubleshooting by observing that both SDK versions were installed in the workload manifest, then forcing the build to use the compatible version.
