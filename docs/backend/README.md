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
Environment variables for Datadog APM are configured via the `.csproj.user` file (gitignored).

**Quick Setup:**
1. Install [Datadog .NET Tracer MSI](https://github.com/DataDog/dd-trace-dotnet/releases)
2. Run the setup script (as Administrator):
   ```powershell
   .\ApiFramework\setup-debug-env.ps1
   ```
3. Open project in Visual Studio and press F5

The setup script configures your local `.csproj.user` file with Datadog environment variables. Visual Studio automatically applies these when debugging with IIS Express.

**Alternative approaches:**
- Use `.\ApiFramework\enable-datadog-profiling.ps1` to set `COR_ENABLE_PROFILING=1` globally
- Use `.\ApiFramework\launch-vs-with-datadog.bat` to launch VS with environment variables pre-set

See [IIS Express Datadog Setup](IIS_EXPRESS_DATADOG_SETUP.md) for details and troubleshooting.

## Related Documentation

- **Deployment**: See [deployment/](../deployment/) for Azure and Docker deployment guides
- **Datadog**: See [datadog/](../datadog/) for APM and monitoring configuration
