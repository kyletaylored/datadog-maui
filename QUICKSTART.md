# Quick Start Guide

Get the Datadog MAUI app running in 5 minutes!

## Prerequisites

- [.NET 8+ SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Android Studio or Xcode (for mobile development)

## Step 1: Start the API (30 seconds)

### Option A: Using Docker Compose (Recommended)

```bash
# From the project root
docker-compose up -d
```

### Option B: Using Docker directly

```bash
# From the project root
cd Api
docker build -t datadog-maui-api .
docker run -d -p 5000:8080 --name datadog-api datadog-maui-api
```

## Step 2: Verify API is Running (10 seconds)

```bash
# Test health endpoint
curl http://localhost:5000/health

# Expected output:
# {"status":"healthy","timestamp":"2025-12-29T21:07:48.979Z"}
```

## Step 3: Run the Mobile App (2-3 minutes)

### For Android:

1. Open Android Studio and start an emulator
2. Navigate to the MauiApp directory:
   ```bash
   cd MauiApp
   ```
3. Build and run:
   ```bash
   dotnet build -t:Run -f net9.0-android
   ```

**Note**: The app automatically uses `http://10.0.2.2:5000` to connect to your local API.

### For iOS (macOS only):

⚠️ **Important**: If you're using Xcode 26.0, see [iOS Build Configuration](docs/ios/BUILD_CONFIGURATION.md) for critical setup details.

1. Open Xcode simulator
2. Navigate to the MauiApp directory:
   ```bash
   cd MauiApp
   ```
3. Build and run:
   ```bash
   dotnet build -t:Run -f net10.0-ios
   ```

**Note**: The app automatically uses `http://localhost:5000` to connect to your local API.

## Step 4: Test the App (1 minute)

1. **Dashboard Tab**:
   - Fill in the form:
     - Session Name: "Test Session"
     - Notes: "My first test"
     - Numeric Value: "42.5"
   - Click "Submit"
   - You should see a success message!

2. **Check API Logs**:
   ```bash
   # Using docker-compose
   docker-compose logs -f api

   # Using docker directly
   docker logs datadog-api -f
   ```

   You should see log entries showing your submitted data!

3. **Web Portal Tab**:
   - Switch to the "Web Portal" tab
   - The WebView should load the .NET MAUI documentation

## Step 5: View Submitted Data

```bash
# Query all submitted data
curl http://localhost:5000/data

# Expected output: JSON array of all submissions
```

## Stopping the API

### Using docker-compose:
```bash
docker-compose down
```

### Using docker directly:
```bash
docker stop datadog-api
docker rm datadog-api
```

## Troubleshooting

### API won't start?
- Check if port 5000 is already in use: `lsof -i :5000`
- View container logs: `docker logs datadog-api`

### Mobile app can't connect to API?
- **Android**: Ensure you're using `http://10.0.2.2:5000` (automatic)
- **iOS**: Ensure you're using `http://localhost:5000` (automatic)
- Verify API is running: `curl http://localhost:5000/health`

### MAUI workload not installed?
```bash
dotnet workload install maui
```

### iOS build failing with Xcode 26.0?
See the detailed [iOS Build Configuration](docs/ios/BUILD_CONFIGURATION.md) guide for troubleshooting native linker errors.

## What's Next?

Check out [README.md](README.md) for:
- Detailed architecture explanation
- API endpoint documentation
- Advanced configuration
- Production considerations

### Additional Documentation
- [Documentation Index](docs/README.md) - All guides and references
- [iOS Build Configuration](docs/ios/BUILD_CONFIGURATION.md) - Critical Xcode 26.0 setup
- [iOS Crash Reporting](docs/ios/CRASH_REPORTING.md) - dSYM setup for crash logs
- [Azure Deployment](docs/deployment/AZURE_QUICK_START.md) - Cloud deployment options

## Need Help?

- Review the full [README.md](README.md)
- Check API logs: `docker logs datadog-api -f`
- Check mobile app console output in your IDE

## Architecture Overview

```
┌─────────────────┐         ┌──────────────────┐
│  Mobile App     │         │   Docker API     │
│  (MAUI)         │         │   (ASP.NET)      │
├─────────────────┤         ├──────────────────┤
│ Dashboard Tab   │────────▶│ POST /data       │
│ - Form Input    │         │ GET  /config     │
│ - Validation    │         │ GET  /health     │
│                 │         │                  │
│ Web Portal Tab  │         │ In-Memory Store  │
│ - WebView       │         │ Logging          │
│ - Dynamic URL   │         │ Telemetry        │
└─────────────────┘         └──────────────────┘
     │                              │
     │  Android: 10.0.2.2:5000      │
     │  iOS: localhost:5000         │
     └──────────────────────────────┘
```

Happy coding!
