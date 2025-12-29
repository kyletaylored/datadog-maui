# Datadog MAUI Application

A cross-platform mobile application (Android/iOS) built with .NET MAUI that allows users to submit data and view web content, integrated with a containerized ASP.NET Core Web API backend.

## Quick Start with Makefile

All commands are available through `make`:

```bash
make help              # See all available commands
make all               # Build everything
make api-start         # Start the API
make app-run-android   # Run on Android
make status            # Check everything
```

See [MAKEFILE_GUIDE.md](MAKEFILE_GUIDE.md) for complete Makefile documentation.

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

- **.NET 8 or 9 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **.NET MAUI Workload** - Install with:
  ```bash
  dotnet workload install maui
  ```
- **Android SDK** (for Android development)
- **Xcode** (for iOS development on macOS)

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

1. **Ensure Docker container is running** (see Phase 1)

2. **Build and run the app**:
   ```bash
   cd MauiApp
   dotnet build -t:Run -f net9.0-ios
   ```

   Or use Visual Studio for Mac/Xcode with the iOS target selected.

3. **The app will automatically use** `http://localhost:5000` to connect to your local API

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

### Build Errors

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

## Resources

- [.NET MAUI Documentation](https://docs.microsoft.com/dotnet/maui)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Docker Documentation](https://docs.docker.com)

## License

This is a demo/MVP project for learning and development purposes.
