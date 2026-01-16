# Documentation Index

Complete documentation for the Datadog MAUI Application organized by developer concern.

## 📱 Mobile Development

**[mobile/](mobile/)** - .NET MAUI mobile app (Android/iOS)

- [Mobile Build Configuration](mobile/BUILD_CONFIGURATION.md) - Cross-platform build setup
- [Mobile Debugging](mobile/DEBUGGING.md) - Debugging MAUI apps
- [RUM Configuration](mobile/RUM_CONFIGURATION.md) - Real User Monitoring setup

### iOS Specific
- [iOS Build Configuration](mobile/ios/BUILD_CONFIGURATION.md) - Critical Xcode 26.0 workarounds
- [iOS Crash Reporting](mobile/ios/CRASH_REPORTING.md) - dSYM crash reporting

## 🔧 Backend APIs

**[backend/](backend/)** - .NET Core and .NET Framework APIs

- [.NET Comparison](backend/DOTNET_COMPARISON.md) - Core vs Framework comparison
- [IIS Deployment](backend/IIS_DEPLOYMENT.md) - Deploy .NET Framework API to IIS
- [IIS Troubleshooting](backend/IIS_TROUBLESHOOTING.md) - Common IIS issues
- [Windows Server Testing](backend/WINDOWS_SERVER_TESTING.md) - Testing on Windows Server

## 🚀 Deployment

**[deployment/](deployment/)** - Azure, Docker, and hosting guides

- [Azure Deployment](deployment/AZURE.md) - Container Apps, App Service, Functions
- [Azure Functions Migration](deployment/AZURE_FUNCTIONS.md) - Serverless conversion guide
- [Dockerfile Comparison](deployment/DOCKERFILE_COMPARISON.md) - Standard vs Functions

## 📊 Datadog Observability

**[datadog/](datadog/)** - APM, RUM, and monitoring

- [Datadog Agent Setup](datadog/DATADOG_AGENT_SETUP.md) - Agent installation and configuration
- [Trace and Log Correlation](datadog/TRACE_LOG_CORRELATION.md) - Correlate traces with logs
- [Git Metadata Integration](datadog/GIT_METADATA_INTEGRATION.md) - Git metadata for CI

## 📚 Feature Guides

**[guides/](guides/)** - Cross-cutting feature documentation

- [BUILD.md](guides/BUILD.md) - Building Docker images with Git metadata

## 📦 Archive

**[archive/](archive/)** - Historical documentation and migration guides

---

## Quick Navigation

### Getting Started
1. [Main README](../README.md) - Project overview and quick start
2. [Quickstart Guide](../QUICKSTART.md) - .NET Core API quick start
3. [Framework Quick Start](FRAMEWORK_QUICKSTART.md) - .NET Framework API quick start

### Common Tasks

**Mobile Development:**
- [Configure iOS builds](mobile/ios/BUILD_CONFIGURATION.md)
- [Set up iOS crash reporting](mobile/ios/CRASH_REPORTING.md)
- [Configure mobile RUM](mobile/RUM_CONFIGURATION.md)
- [Debug MAUI apps](mobile/DEBUGGING.md)

**Backend Development:**
- [Compare .NET implementations](backend/DOTNET_COMPARISON.md)
- [Deploy to IIS](backend/IIS_DEPLOYMENT.md)
- [Troubleshoot IIS](backend/IIS_TROUBLESHOOTING.md)

**Deployment:**
- [Deploy to Azure](deployment/AZURE.md)
- [Migrate to Azure Functions](deployment/AZURE_FUNCTIONS.md)
- [Build Docker images](guides/BUILD.md)

**Datadog:**
- [Set up Datadog Agent](datadog/DATADOG_AGENT_SETUP.md)
- [Configure trace correlation](datadog/TRACE_LOG_CORRELATION.md)
- [Add Git metadata](datadog/GIT_METADATA_INTEGRATION.md)
