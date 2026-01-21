# Datadog APM Patterns for .NET Framework

This document explains the scalable patterns used for Datadog APM instrumentation in the ApiFramework project.

## Architecture Overview

The implementation supports **both** Global.asax and OWIN pipeline modes with minimal code duplication.

### Components

1. **OWIN Middleware** ([Startup.cs](Startup.cs)) - Captures span early in OWIN pipeline
2. **Action Filter** ([Filters/DatadogSpanAttribute.cs](Filters/DatadogSpanAttribute.cs)) - Caches span per request
3. **Base Controller** ([Controllers/DatadogApiController.cs](Controllers/DatadogApiController.cs)) - Provides easy span access

## Scalability Features

### 1. Span Caching with Action Filter

**Problem:** Calling `Tracer.Instance.ActiveScope` in every controller method is inefficient when you have many controllers and methods.

**Solution:** Use a global action filter that runs **once per request** and caches the span in request properties.

```csharp
// Registered globally in WebApiConfig.cs and Startup.cs
config.Filters.Add(new Filters.DatadogSpanAttribute());
```

**Benefits:**
- ✅ Span lookup happens **once per request** (not per controller method)
- ✅ Cached in `Request.Properties["datadog.span"]`
- ✅ Works across all controllers automatically
- ✅ No performance penalty with many controllers

### 2. Base Controller Pattern

All controllers inherit from `DatadogApiController` which provides:

```csharp
public abstract class DatadogApiController : ApiController
{
    protected ISpan GetDatadogSpan()
    {
        // 1. Try cached span (set by filter) - FASTEST
        // 2. Fallback to Tracer.Instance.ActiveScope
        // 3. Fallback to OWIN context
    }
}
```

**Benefits:**
- ✅ Consistent span access across all controllers
- ✅ Automatic fallbacks for different pipeline modes
- ✅ Single method to maintain

### 3. Controller Usage

Controllers simply call `GetDatadogSpan()`:

```csharp
public class AuthController : DatadogApiController
{
    [HttpPost]
    [Route("login")]
    public IHttpActionResult Login([FromBody] LoginRequest request)
    {
        var span = GetDatadogSpan(); // Fast - uses cached span

        if (span != null)
        {
            span.ResourceName = "POST /auth/login";
            span.SetTag("custom.operation.type", "user_login");
        }

        // ... rest of logic
    }
}
```

## Performance Characteristics

### Without Filter (Original Approach)
```
Request
  → Controller Method 1: Tracer.Instance.ActiveScope (lookup)
  → Controller Method 2: Tracer.Instance.ActiveScope (lookup)
  → Controller Method 3: Tracer.Instance.ActiveScope (lookup)
```
**Cost:** O(n) where n = number of times span is accessed

### With Filter (Optimized Approach)
```
Request
  → Action Filter: Tracer.Instance.ActiveScope (lookup once)
  → Store in Request.Properties
  → Controller Method 1: Request.Properties["datadog.span"] (dictionary lookup)
  → Controller Method 2: Request.Properties["datadog.span"] (dictionary lookup)
  → Controller Method 3: Request.Properties["datadog.span"] (dictionary lookup)
```
**Cost:** O(1) per access after initial lookup

## Scaling to Many Controllers

### Adding a New Controller

1. Inherit from `DatadogApiController`:
```csharp
public class MyController : DatadogApiController { }
```

2. Use `GetDatadogSpan()` in action methods:
```csharp
[HttpGet]
public IHttpActionResult MyAction()
{
    var span = GetDatadogSpan();
    if (span != null)
    {
        span.SetTag("custom.action", "my_action");
    }
    // ...
}
```

That's it! The filter automatically handles span caching.

### No Configuration Needed

The filter is registered **globally** in:
- `WebApiConfig.cs` (for Global.asax mode)
- `Startup.cs` (for OWIN mode)

It applies to **all controllers** automatically.

## Pipeline Mode Support

### Global.asax Mode (Traditional ASP.NET)
- Filter registered in `WebApiConfig.Register()`
- `Tracer.Instance.ActiveScope` returns the span
- Works with `aspnet.web_request` spans

### OWIN Mode
- Filter registered in `Startup.Configuration()`
- OWIN middleware captures span early
- Stored in OWIN context
- `GetDatadogSpan()` checks multiple sources
- Works with `aspnet.request` spans

Both modes use the **same controller code** - no changes needed when switching pipelines!

## Custom Span Attributes Best Practices

### 1. Use Consistent Prefixes

All custom tags use `custom.` prefix:
```csharp
span.SetTag("custom.operation.type", "user_login");
span.SetTag("custom.auth.username", username);
span.SetTag("custom.user.id", userId);
```

### 2. Set Resource Name Early

```csharp
var span = GetDatadogSpan();
if (span != null)
{
    // Set resource name first
    span.ResourceName = "POST /auth/login";

    // Then add custom attributes
    span.SetTag("custom.operation.type", "user_login");
}
```

### 3. Tag Business Context

Add tags that help with:
- **Filtering**: `custom.operation.type = "user_login"`
- **Grouping**: `custom.user.id = "12345"`
- **Debugging**: `custom.auth.success = "true"`
- **Pipeline identification**: `custom.pipeline = "owin"`

### 4. Avoid PII in Tags

Don't log:
- Passwords
- Full credit card numbers
- SSNs
- Other sensitive data

## Testing Both Pipeline Modes

### Switch to OWIN Mode
1. Add `USE_OWIN` to **Project Properties → Build → Conditional compilation symbols**
2. Rebuild

### Switch to Global.asax Mode
1. Remove `USE_OWIN` from compilation symbols
2. Rebuild

Controllers work the same in both modes!

## Troubleshooting

### Span is Null

**Check:**
1. Is Datadog tracer installed and running?
2. Run `dd-dotnet check process <PID>` to verify
3. Check debug output for `[Datadog OWIN] Captured span` messages (OWIN mode)
4. Verify filter is registered in WebApiConfig/Startup

### Tags Not Appearing

**Check:**
1. Verify `span != null` before calling `SetTag()`
2. Check Datadog UI for correct service name
3. Ensure span is accessed via `GetDatadogSpan()` not direct tracer access
4. Look for debug output showing span capture

### Performance Concerns

The filter runs once per request with minimal overhead:
- Single dictionary insert per request
- Dictionary lookups are O(1)
- No reflection or complex logic
- Negligible performance impact

## Summary

This architecture provides:
- ✅ **Scalability**: O(1) span access regardless of controller count
- ✅ **Maintainability**: Single base controller + filter pattern
- ✅ **Flexibility**: Works in both Global.asax and OWIN modes
- ✅ **Performance**: Span cached once per request
- ✅ **Simplicity**: Controllers just call `GetDatadogSpan()`

Perfect for applications with **many controllers** and **heavy instrumentation** needs.
