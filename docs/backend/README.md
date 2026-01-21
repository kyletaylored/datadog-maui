# Backend Documentation

Backend API documentation for both .NET Core and .NET Framework implementations.

## .NET Comparison

- [.NET Core vs .NET Framework](DOTNET_COMPARISON.md) - Side-by-side comparison of the two API implementations

## .NET Framework 4.8 API

Windows-specific deployment and development guides:

- [IIS Deployment](IIS_DEPLOYMENT.md) - Complete guide for deploying to IIS
- [IIS Troubleshooting](IIS_TROUBLESHOOTING.md) - Common IIS issues and solutions
- [IIS Express Datadog Setup](IIS_EXPRESS_DATADOG_SETUP.md) - Configure Datadog APM for IIS Express (Visual Studio)
- [Windows Server Testing](WINDOWS_SERVER_TESTING.md) - Testing on Windows Server environments
- [Datadog APM Patterns](DATADOG_PATTERNS.md) - Scalable patterns for APM instrumentation, OWIN span handling, and controller migration

## Quick Setup

The ApiFramework project is pre-configured for Datadog integration with minimal setup required.

### Browser RUM
RUM credentials are automatically loaded from `.env` during build:
- Build runs `generate-rum-config.ps1` to create `rum-config.js`
- Browser loads RUM SDK with credentials from the generated file
- **No manual configuration needed!**

### APM Tracing (IIS Express)
Launch Visual Studio with Datadog environment variables using the provided batch file.

**Quick Setup:**
1. Install [Datadog .NET Tracer MSI](https://github.com/DataDog/dd-trace-dotnet/releases)
2. Launch Visual Studio with environment:
   ```powershell
   .\ApiFramework\launch-vs-with-datadog.bat
   ```
3. Press F5 to debug - APM will work automatically

The batch file auto-detects your Visual Studio installation (any version/edition) and launches it with all required Datadog environment variables. IIS Express processes inherit these variables.

See [IIS Express Datadog Setup](IIS_EXPRESS_DATADOG_SETUP.md) for details and troubleshooting.

## Related Documentation

- **Deployment**: See [deployment/](../deployment/) for Azure and Docker deployment guides
- **Datadog**: See [datadog/](../datadog/) for APM and monitoring configuration
