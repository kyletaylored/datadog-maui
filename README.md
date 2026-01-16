# Datadog MAUI Application

A cross-platform mobile application (Android/iOS) built with .NET MAUI, integrated with ASP.NET Web API backends. Features comprehensive Datadog APM instrumentation across mobile and backend services.

## Quick Start

Use the Makefile for common operations:

```bash
make help              # See all available commands
make all               # Build everything
make api-start         # Start the .NET Core API
make app-run-android   # Run on Android
make status            # Check everything
```

## Project Structure

```
datadog-maui/
├── MauiApp/                   # .NET MAUI Mobile App (Android/iOS)
├── Api/                       # ASP.NET Core 9.0 API (Minimal APIs, Docker)
├── ApiFramework/              # ASP.NET .NET Framework 4.8 API (Web API, IIS)
└── scripts/                   # PowerShell deployment automation
```

## APIs: Two Implementations

This project includes both .NET Core and .NET Framework versions of the same API with identical functionality:

### .NET Core 9.0 API (Minimal APIs)
- **Platform**: Cross-platform (Linux, Windows, macOS)
- **Hosting**: Docker containers, Azure Container Apps
- **Architecture**: Minimal APIs pattern
- **Location**: [Api/](Api/)
- **Quick Start**: [Run with Docker](#run-the-net-core-api)
- **Documentation**: [Quickstart Guide](QUICKSTART.md)

### .NET Framework 4.8 API (Web API Controllers)
- **Platform**: Windows only
- **Hosting**: IIS, IIS Express
- **Architecture**: Web API Controllers pattern
- **Location**: [ApiFramework/](ApiFramework/)
- **Quick Start**: [Framework Quick Start](docs/FRAMEWORK_QUICKSTART.md)
- **Documentation**: [IIS Deployment Guide](docs/IIS_DEPLOYMENT.md)

**See**: [.NET Comparison Guide](docs/DOTNET_COMPARISON.md) for detailed side-by-side comparison.

## Features

### Mobile Application
- Cross-platform (Android/iOS) with .NET MAUI
- Dashboard with data submission form
- WebView portal with dynamic URL loading
- Full Datadog RUM and APM instrumentation
- Platform-specific connectivity (Android: `10.0.2.2:5000`, iOS: `localhost:5000`)

### Backend APIs
- Health checks, data submission, configuration endpoints
- Authentication with session management (login/logout/profile)
- Custom span attributes for detailed tracing
- CORS enabled for cross-origin requests
- In-memory storage (demo/development)

## Prerequisites

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- **.NET MAUI Workload 10.0.1**: `dotnet workload install maui`
- **Docker Desktop** (for .NET Core API) - [Download](https://www.docker.com/products/docker-desktop)
- **IIS** (for .NET Framework API, Windows only)
- **Android SDK** (for Android development)
- **Xcode 26.0** (for iOS development, macOS only)

⚠️ **iOS Build Configuration**: The project includes critical workarounds for Xcode 26.0. See [iOS Build Configuration](docs/ios/BUILD_CONFIGURATION.md) for details.

## Getting Started

### Run the .NET Core API

```bash
cd Api
docker build -t datadog-maui-api .
docker run -d -p 5000:8080 --name datadog-api datadog-maui-api

# Verify
curl http://localhost:5000/health
```

### Run the .NET Framework API

See [Framework Quick Start](docs/FRAMEWORK_QUICKSTART.md) for IIS Express setup.

### Run the Mobile App

**Android**:
```bash
cd MauiApp
dotnet build -t:Run -f net9.0-android
```

**iOS** (macOS only):
```bash
cd MauiApp
dotnet build -t:Run -f net10.0-ios
```

See platform-specific guides for detailed setup:
- [Mobile Build Configuration](docs/mobile/BUILD_CONFIGURATION.md)
- [Mobile Debugging](docs/mobile/DEBUGGING.md)
- [iOS Build Configuration](docs/mobile/ios/BUILD_CONFIGURATION.md)

## API Endpoints

All endpoints available in both .NET Core and .NET Framework APIs:

- `GET /health` - Health check
- `GET /config` - Dynamic configuration
- `POST /auth/login` - User authentication
- `POST /auth/logout` - User logout
- `GET /profile` - Get user profile
- `PUT /profile` - Update user profile
- `POST /data` - Submit form data
- `GET /data` - Retrieve all data

## Documentation

### Quick References
- [Quickstart Guide](QUICKSTART.md) - .NET Core API
- [Framework Quick Start](docs/FRAMEWORK_QUICKSTART.md) - .NET Framework API
- [Documentation Index](docs/README.md) - Complete documentation

### Platform Guides
- **iOS**: [Build Configuration](docs/mobile/ios/BUILD_CONFIGURATION.md) | [Crash Reporting](docs/mobile/ios/CRASH_REPORTING.md)
- **Deployment**: [IIS Guide](docs/backend/IIS_DEPLOYMENT.md) | [Azure Guide](docs/deployment/AZURE.md)
- **Comparison**: [.NET Core vs Framework](docs/backend/DOTNET_COMPARISON.md)

### Configuration & Setup
- [Datadog Agent Setup](docs/datadog/DATADOG_AGENT_SETUP.md)
- [IIS Troubleshooting](docs/backend/IIS_TROUBLESHOOTING.md)
- [Deployment Scripts](scripts/README.md)

### Feature Guides
- [Git Metadata Integration](docs/datadog/GIT_METADATA_INTEGRATION.md)
- [Trace and Log Correlation](docs/datadog/TRACE_LOG_CORRELATION.md)

## Troubleshooting

### Common Issues

**Android Emulator Connection**:
- Verify API is running: `docker ps`
- Use `http://10.0.2.2:5000` (not `localhost`)
- Check logs: `docker logs datadog-api`

**iOS Simulator Connection**:
- Verify API is running: `docker ps`
- Use `http://localhost:5000`
- Ensure `NSAllowsLocalNetworking` in [Info.plist](MauiApp/Platforms/iOS/Info.plist)

**iOS Build Errors (Xcode 26.0)**:
- See [iOS Build Configuration](docs/ios/BUILD_CONFIGURATION.md)
- Project forces iOS SDK 26.0 via `TargetPlatformVersion=26.0`

**IIS Framework API Issues**:
- See [IIS Troubleshooting Guide](docs/IIS_TROUBLESHOOTING.md)
- Verify Windows authentication is disabled
- Check Datadog profiler environment variables

## Development Notes

- **Storage**: In-memory only (demo). Use persistent database for production.
- **Security**: CORS allows all origins (dev). Restrict in production.
- **HTTPS**: Disabled for local dev. Enable for production.
- **Authentication**: Demo implementation. Use OAuth2/JWT for production.

## License

Demo/MVP project for learning and development purposes.
