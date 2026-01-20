# OWIN vs Global.asax Configuration

This project supports both **traditional ASP.NET pipeline (Global.asax)** and **OWIN middleware pipeline** to allow testing different hosting scenarios.

## Current Mode: Global.asax (Default)

By default, the project uses the traditional ASP.NET pipeline via `Global.asax.cs`.

## Switching to OWIN Mode

To replicate OWIN-based customer environments:

### 1. Add Conditional Compilation Symbol

**In Visual Studio:**
1. Right-click project → **Properties**
2. Go to **Build** tab
3. In **Conditional compilation symbols**, add: `USE_OWIN`
4. Click **Save**

**Result:**
```
Debug mode: TRACE;DEBUG;USE_OWIN
Release mode: TRACE;USE_OWIN
```

### 2. Restore NuGet Packages

```powershell
nuget restore
```

Or in Visual Studio: Right-click solution → **Restore NuGet Packages**

### 3. Rebuild Project

```
Build → Rebuild Solution
```

### 4. Run and Verify

When you run the application (F5), check the **Debug Output** window:

**OWIN Mode:**
```
[Global.asax] USE_OWIN is defined - Skipping Web API config (handled by OWIN Startup)
[OWIN] Startup configured - OWIN pipeline is active
[OWIN] GET /health
[OWIN] Response: 200
```

**Global.asax Mode:**
```
[Global.asax] Configuring Web API via traditional ASP.NET pipeline
[Global.asax] BeginRequest: GET /health
```

## Switching Back to Global.asax

1. Remove `USE_OWIN` from **Conditional compilation symbols**
2. Rebuild the project

## How It Works

### OWIN Mode (USE_OWIN defined)

1. **Startup.cs** is discovered via `[assembly: OwinStartup]` attribute
2. OWIN middleware pipeline handles requests
3. `Startup.Configuration()` configures Web API
4. Global.asax still exists but skips Web API configuration
5. Both pipelines run (ASP.NET + OWIN)

### Global.asax Mode (Default)

1. **Global.asax.cs** `Application_Start()` configures Web API
2. Traditional ASP.NET pipeline handles requests
3. `Startup.cs` is ignored (no OWIN startup attribute processed)
4. Single pipeline (ASP.NET only)

## Key Differences for Testing

### Request Pipeline

**OWIN:**
- Request → IIS → ASP.NET Pipeline → OWIN Middleware → Web API
- Two pipelines running together
- OWIN middleware can intercept before Web API

**Global.asax:**
- Request → IIS → ASP.NET Pipeline → Web API
- Single pipeline
- All handled by ASP.NET events

### Datadog APM Behavior

Both modes should work identically with Datadog APM because:
- CLR profiler instruments at the .NET level (not pipeline-specific)
- Auto-instrumentation hooks into both pipelines
- Custom tags via `Tracer.Instance.ActiveScope` work in both

### Use Cases

**Test with OWIN when:**
- Customer uses OWIN middleware
- Testing self-hosting scenarios
- Replicating OWIN-specific issues
- Using OAuth/OWIN authentication middleware

**Test with Global.asax when:**
- Customer uses traditional ASP.NET
- Simpler IIS-only hosting
- Default/standard configuration
- Pure Web API without middleware

## Files Involved

| File | Purpose |
|------|---------|
| `Startup.cs` | OWIN configuration (active when `USE_OWIN` defined) |
| `Global.asax.cs` | ASP.NET configuration (always active, conditionally configures Web API) |
| `App_Start/WebApiConfig.cs` | Web API routes and settings (shared by both) |
| `Web.config` | IIS and ASP.NET configuration (both modes) |

## Troubleshooting

### Both Pipelines Active

If you see both `[OWIN]` and `[Global.asax]` logs, that's **normal** when using OWIN in IIS. Both pipelines run together.

### OWIN Not Starting

1. Verify `USE_OWIN` is in compilation symbols
2. Check NuGet packages are restored
3. Rebuild solution (not just build)
4. Check Debug Output for `[OWIN] Startup configured` message

### Datadog Traces Missing

- Works the same in both modes
- If traces work in Global.asax mode but not OWIN, check customer's OWIN middleware ordering
- OWIN middleware runs **after** Datadog instrumentation, so it shouldn't interfere

## Recommended Testing Approach

1. **Default**: Test with Global.asax (simpler, most common)
2. **Customer issue**: If customer uses OWIN, enable `USE_OWIN` to replicate their environment
3. **Compare**: Test same scenario in both modes to identify pipeline-specific issues
