# Datadog MAUI Application

A cross-platform mobile application (Android/iOS) built with .NET MAUI that allows users to submit data and view web content, integrated with a containerized ASP.NET Core Web API backend.

## ⚠️ Critical iOS Build Configuration

**If you're using Xcode 26.0 or 26.1**: This project includes a critical workaround for iOS builds. The .NET MAUI workload 10.0.1 includes both iOS SDK 26.0 and 26.2, but defaults to 26.2 (which requires Xcode 26.2). This project is configured to force iOS SDK 26.0 via `TargetPlatformVersion=26.0` in the csproj.

**Why this matters**: Without this configuration, iOS builds will fail with cryptic native linker errors (`Undefined symbols: _main`). This configuration is already in place in [DatadogMauiApp.csproj](MauiApp/DatadogMauiApp.csproj#L25-L40).

📖 **Full details**: [iOS Build Configuration](docs/ios/BUILD_CONFIGURATION.md)

## Quick Start with Makefile

All commands are available through `make`:

```bash
make help              # See all available commands
make all               # Build everything
make api-start         # Start the API
make app-run-android   # Run on Android
make status            # Check everything
```

Run `make help` to see all available commands.

## Project Structure

```
datadog-maui/
├── Api/                        # ASP.NET Core Web API
│   ├── Models/                 # Data models
│   ├── Program.cs             # API endpoints and configuration
│   ├── Dockerfile             # Container definition
│   └── DatadogMauiApi.csproj  # API project file
│
└── MauiApp/                   # .NET MAUI Mobile App
    ├── Models/                # Shared data models
    ├── Services/              # API service layer
    ├── Pages/                 # XAML pages
    │   ├── DashboardPage      # Data input form
    │   └── WebPortalPage      # WebView component
    ├── Resources/             # App resources
    ├── Platforms/             # Platform-specific code
    │   ├── Android/
    │   └── iOS/
    └── DatadogMauiApp.csproj  # MAUI project file
```

## Features

### Mobile Application (.NET MAUI)
- **Dashboard Tab**: Input form with validation
  - Session Name (text)
  - Notes (text area)
  - Numeric Value (number input)
  - Submit button with validation and feedback

- **Web Portal Tab**: WebView with dynamic URL loading
  - Loads URL from API configuration
  - Pull-to-refresh support
  - Loading indicator

- **Telemetry Tracking**:
  - Unique CorrelationID for each API request
  - Event logging (App Start, Form Submit, Tab Changes)
  - Console logging for debugging

### Backend API (.NET 8/9)
- **Endpoints**:
  - `GET /health` - Health check endpoint (returns 200 OK)
  - `POST /data` - Accepts form submissions with CorrelationID
  - `GET /config` - Returns dynamic configuration (WebView URL, feature flags)
  - `GET /data` - Debug endpoint to view all submissions

- **Features**:
  - In-memory data storage (ConcurrentBag)
  - Structured logging for all requests
  - CORS enabled for cross-origin requests
  - Dockerized for easy deployment

## Architecture Strategy

**"Local-First" Development**: The API is containerized from the start, allowing the mobile emulator to communicate with it as if it were a remote server. This minimizes friction when moving to cloud deployment.

## Platform-Specific Connectivity

The mobile app automatically detects the platform and uses the appropriate base URL:

- **Android Emulator**: `http://10.0.2.2:5000` (maps to host machine's localhost)
- **iOS Simulator**: `http://localhost:5000` (direct access)

This is handled in [ApiService.cs:31-40](MauiApp/Services/ApiService.cs#L31-L40).

## Prerequisites

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **.NET MAUI Workload 10.0.1** - Install with:
  ```bash
  dotnet workload install maui
  ```
- **Android SDK** (for Android development)
- **Xcode 26.0** (for iOS development on macOS)
  - ⚠️ **Important iOS Build Configuration**: See [iOS Build Notes](MauiApp/iOS_BUILD_NOTES.md) for critical setup details

## Getting Started

### Phase 1: Run the API in Docker

1. **Build the Docker image**:
   ```bash
   cd Api
   docker build -t datadog-maui-api .
   ```

2. **Run the container**:
   ```bash
   docker run -d -p 5000:8080 --name datadog-api datadog-maui-api
   ```

3. **Verify the API is running**:
   ```bash
   # Health check
   curl http://localhost:5000/health

   # Get configuration
   curl http://localhost:5000/config

   # Test data submission
   curl -X POST http://localhost:5000/data \
     -H "Content-Type: application/json" \
     -d '{
       "correlationId": "test-123",
       "sessionName": "Test Session",
       "notes": "Testing API",
       "numericValue": 42.5
     }'
   ```

4. **View logs**:
   ```bash
   docker logs datadog-api -f
   ```

5. **Stop/Remove container**:
   ```bash
   docker stop datadog-api
   docker rm datadog-api
   ```

### Phase 2: Run the Mobile App

#### Android Emulator

1. **Ensure Docker container is running** (see Phase 1)

2. **Start Android Emulator** via Android Studio or Visual Studio

3. **Build and run the app**:
   ```bash
   cd MauiApp
   dotnet build -t:Run -f net9.0-android
   ```

   Or use Visual Studio/VS Code with the Android target selected.

4. **The app will automatically use** `http://10.0.2.2:5000` to connect to your local API

#### iOS Simulator (macOS only)

⚠️ **Important**: iOS builds require specific configuration for Xcode 26.0. See [iOS Build Configuration](docs/ios/BUILD_CONFIGURATION.md) for details.

1. **Ensure Docker container is running** (see Phase 1)

2. **Build and run the app**:
   ```bash
   cd MauiApp
   dotnet build -t:Run -f net10.0-ios
   ```

   Or use Visual Studio for Mac/Xcode with the iOS target selected.

3. **The app will automatically use** `http://localhost:5000` to connect to your local API

**Key iOS Configuration**: The project is configured to use iOS SDK 26.0 (compatible with Xcode 26.0) instead of the default iOS SDK 26.2. This is achieved by setting `TargetPlatformVersion=26.0` in [DatadogMauiApp.csproj](MauiApp/DatadogMauiApp.csproj#L29). Both SDK versions are included in the MAUI workload 10.0.1, but the build system defaults to 26.2 which requires Xcode 26.2.

## Testing the Integration

1. **Start the API container** (Phase 1, Step 2)

2. **Launch the mobile app** on your emulator/simulator

3. **Test Dashboard Tab**:
   - Fill in the form fields
   - Click "Submit"
   - Check Docker logs to see the received data:
     ```bash
     docker logs datadog-api -f
     ```

4. **Test Web Portal Tab**:
   - Switch to the Web Portal tab
   - Verify the WebView loads the configured URL
   - Check console logs for telemetry events

## API Endpoints

### GET /health
Returns API health status.

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2025-12-29T21:07:48.979Z"
}
```

### GET /config
Returns dynamic configuration for the mobile app.

**Response**:
```json
{
  "webViewUrl": "https://docs.microsoft.com/dotnet/maui",
  "featureFlags": {
    "EnableTelemetry": true,
    "EnableAdvancedFeatures": false
  }
}
```

### POST /data
Submits form data from the mobile app.

**Request Body**:
```json
{
  "correlationId": "guid-here",
  "sessionName": "My Session",
  "notes": "Some notes",
  "numericValue": 42.5
}
```

**Response**:
```json
{
  "message": "Data received successfully",
  "correlationId": "guid-here",
  "timestamp": "2025-12-29T21:08:01.646Z"
}
```

### GET /data
Returns all submitted data (debug endpoint).

**Response**:
```json
[
  {
    "correlationId": "guid-here",
    "sessionName": "My Session",
    "notes": "Some notes",
    "numericValue": 42.5
  }
]
```

## Troubleshooting

### Android Emulator Cannot Connect to API

1. Verify the container is running: `docker ps`
2. Check the container logs: `docker logs datadog-api`
3. Ensure you're using `http://10.0.2.2:5000` (not `localhost`)
4. Check Android emulator network settings

### iOS Simulator Cannot Connect to API

1. Verify the container is running: `docker ps`
2. Check that port 5000 is exposed: `docker port datadog-api`
3. Ensure `NSAllowsLocalNetworking` is enabled in [Info.plist:36-38](MauiApp/Platforms/iOS/Info.plist#L36-L38)

### MAUI Workload Not Installed

If you get template errors, install the MAUI workload:
```bash
dotnet workload install maui
```

### iOS Build Errors with Xcode 26.0

If you encounter native linker errors like `Undefined symbols: _main` on iOS:

**Root Cause**: The MAUI workload includes both iOS SDK 26.0 and 26.2. By default, it uses SDK 26.2 which requires Xcode 26.2. If you have Xcode 26.0 installed, the native linker will fail.

**Solution**: The project is already configured to force iOS SDK 26.0 via `TargetPlatformVersion=26.0` in [DatadogMauiApp.csproj](MauiApp/DatadogMauiApp.csproj#L29). If you still encounter issues:

1. Verify both SDK versions are installed:
   ```bash
   dotnet workload list
   # Should show:
   # Microsoft.iOS.Sdk.net10.0_26.0 version 26.0.11017
   # Microsoft.iOS.Ref.net10.0_26.2 version 26.2.10191
   ```

2. Check Xcode version:
   ```bash
   xcodebuild -version
   # Should be: Xcode 26.0.x
   ```

3. See detailed troubleshooting in [iOS Build Configuration](docs/ios/BUILD_CONFIGURATION.md)

### General Build Errors

1. Clean the solution:
   ```bash
   dotnet clean
   rm -rf bin/ obj/
   ```

2. Restore packages:
   ```bash
   dotnet restore
   ```

3. Rebuild:
   ```bash
   dotnet build
   ```

## Development Notes

### In-Memory Storage
The API currently uses in-memory storage (`ConcurrentBag`) for the MVP phase. Data is lost when the container restarts. For production, replace this with a persistent database (SQL Server, PostgreSQL, etc.).

### Telemetry
All telemetry events are logged to the console. In production, integrate with a proper telemetry system like Application Insights, Datadog, or similar.

### Security
- The API allows all CORS origins for development. Restrict this in production.
- HTTPS is disabled for local development. Enable HTTPS for production deployment.
- Add authentication/authorization before deploying to production.

## Next Steps

### Phase 4: Cloud Deployment
- Deploy API to Azure Container Apps, AWS ECS, or similar
- Update mobile app base URLs to point to cloud endpoints
- Add environment-specific configuration

### Phase 5: Enhancements
- Add persistent database (Entity Framework Core + SQL Server/PostgreSQL)
- Implement authentication (OAuth2, JWT)
- Add offline support with local SQLite
- Implement proper error handling and retry logic
- Add unit and integration tests
- Set up CI/CD pipelines

## Documentation

### Main Guides
- [Quickstart Guide](QUICKSTART.md) - Get started quickly
- [Documentation Index](docs/README.md) - All documentation

### Setup & Configuration
- [Datadog Agent Setup](docs/setup/DATADOG_AGENT_SETUP.md)
- [Setup Status](docs/setup/SETUP_COMPLETE.md)

### iOS Development
- [iOS Build Configuration](docs/ios/BUILD_CONFIGURATION.md) - Critical Xcode 26.0 setup
- [iOS SDK Version Fix](docs/ios/SDK_VERSION_FIX.md) - Quick reference for SDK 26.0 workaround
- [iOS dSYM Crash Reporting](docs/ios/CRASH_REPORTING.md) - Crash log symbolication guide
- [Upload dSYMs Script](docs/ios/scripts/upload-dsyms.sh) - Automation script for dSYM uploads

### Deployment
- [Azure Quick Start](docs/deployment/AZURE_QUICK_START.md) - Quick decision guide for Azure deployment
- [Azure Functions Migration](docs/deployment/AZURE_FUNCTIONS_MIGRATION.md) - Migrate API to Azure Functions
- [Dockerfile Comparison](docs/deployment/DOCKERFILE_COMPARISON.md) - Standard vs Azure Functions containers

### Feature Guides
- [Building with Git Metadata](docs/guides/BUILD.md)
- [Trace and Log Correlation](docs/guides/TRACE_LOG_CORRELATION.md)
- [Git Metadata Integration](docs/guides/GIT_METADATA_INTEGRATION.md)

### External Resources
- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Docker Documentation](https://docs.docker.com)
- [Datadog .NET APM](https://docs.datadoghq.com/tracing/setup_overview/setup/dotnet-core/)

## License

This is a demo/MVP project for learning and development purposes.
