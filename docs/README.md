# Documentation Index

Complete documentation for the Datadog MAUI Application organized by developer concern.

## 📑 Table of Contents

- [Quick Start Guides](#-quick-start-guides)
- [Mobile Development](#-mobile-development)
- [Backend APIs](#-backend-apis)
- [Deployment](#-deployment)
- [Datadog Observability](#-datadog-observability)
- [Feature Guides](#-feature-guides)
- [Archive](#-archive)
- [Find Documentation By Task](#-find-documentation-by-task)
- [Documentation Conventions](#-documentation-conventions)

---

## 🚀 Quick Start Guides

| Guide                                                 | Description                             | Platform            |
| ----------------------------------------------------- | --------------------------------------- | ------------------- |
| [Main README](../README.md)                           | Project overview and quick start        | All                 |
| [.NET Core Quick Start](../QUICKSTART.md)             | Get started with the .NET Core API      | Linux/macOS/Windows |
| [.NET Framework Quick Start](FRAMEWORK_QUICKSTART.md) | Get started with the .NET Framework API | Windows             |

---

## Mobile Development

### General Mobile Docs

- [Mobile README](mobile/README.md) - Mobile documentation index
- [Mobile Build Configuration](mobile/BUILD_CONFIGURATION.md) - Cross-platform MAUI build setup
- [Mobile Debugging](mobile/DEBUGGING.md) - Debugging MAUI applications
- [RUM Configuration](mobile/RUM_CONFIGURATION.md) - Real User Monitoring setup

### iOS Specific

- [iOS Build Configuration](mobile/ios/BUILD_CONFIGURATION.md) - ⚠️ **Critical** Xcode 26.0 workarounds
- [iOS Crash Reporting](mobile/ios/CRASH_REPORTING.md) - dSYM upload and crash symbolication
- [iOS SDK Version Fix](mobile/ios/SDK_VERSION_FIX.md) - SDK version troubleshooting

---

## Backend APIs

### Backend Overview

- [Backend README](backend/README.md) - Backend documentation index
- [.NET Comparison](backend/DOTNET_COMPARISON.md) - Core vs Framework comparison

### .NET Framework 4.8 (Windows/IIS)

- [IIS Deployment](backend/IIS_DEPLOYMENT.md) - Complete IIS deployment guide
- [IIS Express Datadog Setup](backend/IIS_EXPRESS_DATADOG_SETUP.md) - ⭐ APM for Visual Studio debugging
- [IIS Troubleshooting](backend/IIS_TROUBLESHOOTING.md) - Common IIS issues and solutions
- [Windows Server Testing](backend/WINDOWS_SERVER_TESTING.md) - Testing on Windows Server
- [Datadog APM Patterns](backend/DATADOG_PATTERNS.md) - ⭐ Scalable APM instrumentation, OWIN/Global.asax span handling

---

## Deployment

### Azure Deployment

- [Deployment README](deployment/README.md) - Deployment documentation index
- [Azure Deployment](deployment/AZURE.md) - Container Apps, App Service, Functions
- [Azure Functions Migration](deployment/AZURE_FUNCTIONS.md) - Serverless conversion guide
- [Dockerfile Comparison](deployment/DOCKERFILE_COMPARISON.md) - Standard vs Functions containers

### Historical Deployment Guides

- [Azure Deployment (Old)](AZURE_DEPLOYMENT.md) - Legacy Azure deployment guide

---

## Datadog Observability

### Datadog Setup

- [Datadog README](datadog/README.md) - Datadog documentation index
- [Agent Setup](datadog/AGENT_SETUP.md) - Install and configure Datadog Agent
- [Trace & Log Correlation](datadog/TRACE_LOG_CORRELATION.md) - Correlate traces with logs
- [Git Metadata](datadog/GIT_METADATA.md) - Git metadata integration for CI/CD

---

## Feature Guides

### Build & CI/CD

- [Build Guide](guides/BUILD.md) - Building Docker images with Git metadata

### Authentication & Security

- [Authentication and Tracing](AUTHENTICATION_AND_TRACING.md) - Auth implementation with APM

---

## Archive

Historical documentation and migration guides that may still be useful for reference:

### Historical Setup Docs

- [API Datadog Setup](archive/API_DATADOG_SETUP.md)
- [Datadog Setup](archive/DATADOG_SETUP.md)
- [Datadog Setup Updated](archive/DATADOG_SETUP_UPDATED.md)
- [Datadog Advanced Features](archive/DATADOG_ADVANCED_FEATURES.md)

### Historical Status Reports

- [Implementation Status](archive/IMPLEMENTATION_STATUS.md)
- [Integration Status](archive/INTEGRATION_STATUS.md)
- [Final Status](archive/FINAL_STATUS.md)
- [Build Success](archive/BUILD_SUCCESS.md)
- [Correlation Success](archive/CORRELATION_SUCCESS.md)
- [Git Metadata Complete](archive/GIT_METADATA_COMPLETE.md)
- [Setup Complete](archive/SETUP_COMPLETE.md)
- [Ready to Test](archive/READY_TO_TEST.md)

### Historical Troubleshooting

- [Network Fix](archive/NETWORK_FIX.md)
- [MAUI Workload Issue](archive/MAUI_WORKLOAD_ISSUE.md)

### Historical Guides

- [Makefile Guide](archive/MAKEFILE_GUIDE.md)
- [Makefile Migration](archive/MAKEFILE_MIGRATION.md)
- [Test API Only](archive/TEST_API_ONLY.md)
- [Git Setup](archive/GIT_SETUP.md)
- [Project Summary](archive/PROJECT_SUMMARY.md)
- [Instructions](archive/instructions.md)

### Planning Documents

- [Reorganization Plan](REORGANIZATION_PLAN.md) - Documentation reorganization plan

---

## Find Documentation By Task

### I want to...

#### Get Started

- **Set up the project** → [Main README](../README.md)
- **Run the .NET Core API** → [.NET Core Quick Start](../QUICKSTART.md)
- **Run the .NET Framework API** → [.NET Framework Quick Start](FRAMEWORK_QUICKSTART.md)

#### Mobile Development

- **Build iOS app** → [iOS Build Configuration](mobile/ios/BUILD_CONFIGURATION.md) ⚠️ Start here!
- **Build Android app** → [Mobile Build Configuration](mobile/BUILD_CONFIGURATION.md)
- **Debug mobile app** → [Mobile Debugging](mobile/DEBUGGING.md)
- **Set up mobile RUM** → [RUM Configuration](mobile/RUM_CONFIGURATION.md)
- **Fix iOS crashes** → [iOS Crash Reporting](mobile/ios/CRASH_REPORTING.md)

#### Backend Development

- **Compare .NET Core vs Framework** → [.NET Comparison](backend/DOTNET_COMPARISON.md)
- **Deploy to IIS** → [IIS Deployment](backend/IIS_DEPLOYMENT.md)
- **Debug with IIS Express** → [IIS Express Datadog Setup](backend/IIS_EXPRESS_DATADOG_SETUP.md) ⭐
- **Implement scalable APM** → [Datadog APM Patterns](backend/DATADOG_PATTERNS.md) ⭐
- **Troubleshoot IIS** → [IIS Troubleshooting](backend/IIS_TROUBLESHOOTING.md)
- **Test on Windows Server** → [Windows Server Testing](backend/WINDOWS_SERVER_TESTING.md)
- **Switch between Global.asax and OWIN** → [Datadog APM Patterns](backend/DATADOG_PATTERNS.md#switching-between-globalasax-and-owin)

#### Deployment

- **Deploy to Azure** → [Azure Deployment](deployment/AZURE.md)
- **Convert to Azure Functions** → [Azure Functions Migration](deployment/AZURE_FUNCTIONS.md)
- **Build Docker images** → [Build Guide](guides/BUILD.md)
- **Compare Dockerfiles** → [Dockerfile Comparison](deployment/DOCKERFILE_COMPARISON.md)

#### Datadog Setup

- **Install Datadog Agent** → [Agent Setup](datadog/AGENT_SETUP.md)
- **Correlate traces with logs** → [Trace & Log Correlation](datadog/TRACE_LOG_CORRELATION.md)
- **Add Git metadata** → [Git Metadata](datadog/GIT_METADATA.md)
- **Fix OWIN span tags** → [Datadog APM Patterns](backend/DATADOG_PATTERNS.md)

---

## Documentation Conventions

### Priority Indicators

- ⭐ - **Highly recommended** reading for the feature area
- ⚠️ - **Critical** - contains important workarounds or fixes

### Document Categories

- **README.md** - Index files for each category
- **Quick Start** - Get up and running quickly
- **Configuration** - Setup and configuration guides
- **Troubleshooting** - Problem-solving guides
- **Reference** - Detailed technical references
- **Archive** - Historical documents (may be outdated)

### Platform Indicators

- **All** - Cross-platform (Linux, macOS, Windows)
- **Windows** - Windows only
- **Linux/macOS** - Unix-like systems
- **iOS** - iOS development (macOS only)
- **Android** - Android development (all platforms)

---

## Recently Updated

| Document                                                          | Last Major Update | Changes                                                     |
| ----------------------------------------------------------------- | ----------------- | ----------------------------------------------------------- |
| [Datadog APM Patterns](backend/DATADOG_PATTERNS.md)               | Latest            | Added OWIN migration guide, pipeline switching instructions |
| [IIS Express Datadog Setup](backend/IIS_EXPRESS_DATADOG_SETUP.md) | Recent            | Batch file approach for VS launch                           |
| [iOS Build Configuration](mobile/ios/BUILD_CONFIGURATION.md)      | Recent            | Xcode 26.0 workarounds                                      |

---

## Contributing to Documentation

When adding new documentation:

1. Add entry to this documentation index
2. Add entry to the relevant category README (backend/README.md, mobile/README.md, etc.)
3. Link from the main [README.md](../README.md) if it's a common task
4. Use clear, descriptive titles and include platform requirements
5. Add priority indicators (⭐, ⚠️) where appropriate

---

## Feedback

Found an issue or have a suggestion for improving documentation? Please open an issue in the repository
