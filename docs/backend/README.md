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

## Quick Setup

The ApiFramework project is pre-configured for Datadog integration with minimal setup required.

### Browser RUM
RUM credentials are automatically loaded from `.env` during build:
- Build runs `generate-rum-config.ps1` to create `rum-config.js`
- Browser loads RUM SDK with credentials from the generated file
- **No manual configuration needed!**

### APM Tracing (IIS Express)
Datadog environment variables are pre-configured in `Properties/launchSettings.json`:
- All required `COR_*` and `CORECLR_*` variables included
- Automatically applied when you press F5 in Visual Studio
- **No scripts to run!**

**Requirements:**
1. Install [Datadog .NET Tracer MSI](https://github.com/DataDog/dd-trace-dotnet/releases)
2. Press F5 to run

See [IIS Express Datadog Setup](IIS_EXPRESS_DATADOG_SETUP.md) for details and troubleshooting.

## Related Documentation

- **Deployment**: See [deployment/](../deployment/) for Azure and Docker deployment guides
- **Datadog**: See [datadog/](../datadog/) for APM and monitoring configuration
