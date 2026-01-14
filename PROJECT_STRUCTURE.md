# Datadog MAUI Project Structure

This repository contains **two complete implementations** of the Datadog MAUI API for customer demos.

## Overview

```
datadog-maui/
├── Api/              ⭐ .NET Core 9.0 (Modern, Cross-platform)
├── ApiFramework/     ⭐ .NET Framework 4.8 (Traditional, Windows)
├── MauiApp/          📱 Mobile app (.NET MAUI)
├── scripts/          🔧 Deployment scripts
└── docs/             📚 Documentation
```

## Two API Implementations

### 1. .NET Core 9.0 (`Api/`)

**Modern ASP.NET Core with Minimal APIs**

```
Api/
├── Program.cs              # All endpoints (minimal APIs)
├── Models/                 # Data models
├── Services/              # Business logic
│   └── SessionManager.cs
├── wwwroot/               # Web portal (HTML/JS)
├── Dockerfile             # Docker deployment
└── DatadogMauiApi.csproj  # Project file
```

**Characteristics:**
- ✅ Cross-platform (Linux, macOS, Windows)
- ✅ Minimal APIs (less code)
- ✅ Docker-ready
- ✅ 3x faster than Framework
- ✅ Modern C# 13 features

**Best for:**
- New projects
- Cloud-native deployments
- Kubernetes/Docker
- Cross-platform needs

### 2. .NET Framework 4.8 (`ApiFramework/`)

**Traditional ASP.NET Web API with Controllers**

```
ApiFramework/
├── Controllers/                    # Web API 2 controllers
│   ├── AuthController.cs
│   ├── ConfigController.cs
│   ├── DataController.cs
│   ├── HealthController.cs
│   └── ProfileController.cs
├── Models/                         # Data models
├── Services/                       # Business logic
│   └── SessionManager.cs
├── App_Start/                      # Configuration
│   └── WebApiConfig.cs
├── Global.asax                     # App startup
├── Web.config                      # Configuration + Datadog
└── DatadogMauiApi.Framework.csproj # Project file
```

**Characteristics:**
- ✅ Windows IIS deployment
- ✅ Familiar to enterprise .NET teams
- ✅ Full .NET Framework compatibility
- ⚠️ Windows-only
- ⚠️ More boilerplate code

**Best for:**
- Enterprise .NET Framework requirements
- Existing IIS infrastructure
- Legacy system integration
- Windows-only environments

## Feature Comparison

| Feature | .NET Core 9.0 | .NET Framework 4.8 |
|---------|---------------|-------------------|
| **Endpoints** | ✅ All | ✅ All |
| **Authentication** | ✅ | ✅ |
| **Datadog APM** | ✅ | ✅ |
| **Custom Span Tags** | ✅ | ✅ |
| **Distributed Tracing** | ✅ | ✅ |
| **RUM Integration** | ✅ | ✅ |
| **Docker Support** | ✅ | ⚠️ Windows containers only |
| **Cross-platform** | ✅ | ❌ Windows only |
| **Performance** | ✅ Fast | ⚠️ 3x slower |
| **Code Size** | ✅ ~400 lines | ⚠️ ~800 lines |

## Endpoints (Identical in Both)

Both implementations provide the same API:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/health` | Health check |
| GET | `/config` | Configuration |
| POST | `/auth/login` | User login |
| POST | `/auth/logout` | User logout |
| GET | `/profile` | Get user profile |
| PUT | `/profile` | Update user profile |
| POST | `/data` | Submit data |
| GET | `/data` | Get all data |

## Datadog Integration (Identical)

Both implementations have:
- ✅ Automatic APM instrumentation
- ✅ Custom span attributes under `custom.*` namespace
- ✅ Distributed tracing headers
- ✅ Session-based user tracking
- ✅ RUM correlation

## Quick Start

### .NET Core
```bash
cd Api
dotnet restore
dotnet run
# → http://localhost:5000
```

### .NET Framework
```
Open ApiFramework/DatadogMauiApi.Framework.csproj in Visual Studio
Press F5
# → http://localhost:50000
```

## Deployment

### .NET Core
```bash
# Docker
docker build -t datadog-maui-api Api/
docker run -p 5000:8080 datadog-maui-api

# Azure App Service (Linux or Windows)
az webapp create --runtime "DOTNET|9.0"
```

### .NET Framework
```powershell
# IIS (Windows)
msbuild /p:Configuration=Release
# Copy bin/Release/ to IIS folder

# Azure App Service (Windows only)
az webapp create --runtime "DOTNETFRAMEWORK|4.8"
```

## Documentation

- [.NET Core README](Api/README.md)
- [.NET Framework README](ApiFramework/README.md)
- [Detailed Comparison](docs/DOTNET_COMPARISON.md)
- [Azure Deployment](docs/AZURE_DEPLOYMENT.md)
- [Quick Start Guides](FRAMEWORK_QUICKSTART.md)

## Mobile App

```
MauiApp/
├── Platforms/
│   ├── Android/
│   └── iOS/
├── MainPage.xaml      # Mobile UI
└── DatadogMauiApp.csproj
```

**Supports:**
- ✅ Android
- ✅ iOS
- ✅ Datadog Mobile RUM
- ✅ Distributed tracing to backend

## Choosing Which Version to Use

### Use .NET Core 9.0 If:
- ✅ Building new applications
- ✅ Need cross-platform support
- ✅ Want best performance
- ✅ Prefer modern C# features
- ✅ Using Docker/Kubernetes

### Use .NET Framework 4.8 If:
- ✅ Customer mandate for .NET Framework
- ✅ Legacy system integration
- ✅ Existing IIS infrastructure
- ✅ Windows-only environment
- ✅ Team only knows Web API Controllers

### Show Both for Demos!
For customer demos, demonstrate **both implementations** to show:
1. Datadog works with both platforms
2. Migration path available
3. Same monitoring capabilities
4. Let customer choose based on their requirements

## Development Commands

### .NET Core
```bash
make api-build        # Build Docker image
make api-start        # Start containers
make api-logs         # View logs
make api-test         # Test endpoints
make azure-deploy     # Deploy to Azure
```

### .NET Framework
```powershell
# Build
msbuild /p:Configuration=Release

# Test
Invoke-RestMethod http://localhost:50000/health

# Deploy to IIS
Copy-Item bin\Release\* C:\inetpub\wwwroot\api\
```

## Next Steps

1. ✅ Both APIs are ready to run
2. ✅ Full Datadog integration configured
3. ✅ Documentation complete
4. 📖 Read comparison guide: `docs/DOTNET_COMPARISON.md`
5. 🚀 Try deploying both to Azure
6. 📊 Compare traces in Datadog dashboard

## Support

- Issues: GitHub Issues
- Documentation: `docs/` folder
- Examples: Both `Api/` and `ApiFramework/`
