# Datadog APM Patterns for .NET Framework

This document explains the scalable patterns used for Datadog APM instrumentation in the ApiFramework project.

## Architecture Overview

The implementation supports **both** Global.asax and OWIN pipeline modes with minimal code duplication.

### Components

1. **OWIN Middleware** ([Startup.cs](../../ApiFramework/Startup.cs)) - Captures span early in OWIN pipeline
2. **Action Filter** ([Filters/DatadogSpanAttribute.cs](../../ApiFramework/Filters/DatadogSpanAttribute.cs)) - Caches span per request
3. **Base Controller** ([Controllers/DatadogApiController.cs](../../ApiFramework/Controllers/DatadogApiController.cs)) - Provides easy span access

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

## Migration Guide: Adapting Existing Controllers

### Why Migration is Needed for OWIN

When using OWIN, the Datadog tracer creates a different span hierarchy than Global.asax:

**Global.asax Span Hierarchy:**
```
aspnet.web_request (single span for entire request)
```

**OWIN Span Hierarchy:**
```
aspnet.request (parent span - this is what you see in Datadog UI)
  └─ aspnet-webapi.request (child span - created by Web API framework)
```

**The Problem:**
- In Global.asax: `Tracer.Instance.ActiveScope` returns `aspnet.web_request`
- In OWIN: `Tracer.Instance.ActiveScope` returns `aspnet-webapi.request` (the child span)
- Custom tags set on the child span don't appear on the parent span
- Datadog UI shows the parent `aspnet.request` span by default
- **Result:** Your custom tags appear to be missing!

**The Solution:**
The OWIN middleware captures the parent `aspnet.request` span early in the pipeline and stores it in the OWIN context. The filter and base controller provide access to this parent span so your custom tags appear correctly.

### Step-by-Step Migration

#### 1. Add Required Files

Ensure these files exist in your project:

**Controllers/DatadogApiController.cs:**
```csharp
using System.Web.Http;
using Datadog.Trace;

namespace DatadogMauiApi.Framework.Controllers
{
    public abstract class DatadogApiController : ApiController
    {
        protected ISpan GetDatadogSpan()
        {
            // Try cached span from filter
            if (Request.Properties.ContainsKey("datadog.span"))
            {
                return Request.Properties["datadog.span"] as ISpan;
            }

            // Fallback to active scope
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                return activeScope.Span;
            }

            // Fallback to OWIN context
            if (Request.Properties.ContainsKey("MS_OwinContext"))
            {
                var owinContext = Request.Properties["MS_OwinContext"] as Microsoft.Owin.IOwinContext;
                if (owinContext != null && owinContext.Environment.ContainsKey("datadog.span"))
                {
                    return owinContext.Environment["datadog.span"] as ISpan;
                }
            }

            return null;
        }
    }
}
```

**Filters/DatadogSpanAttribute.cs:**
```csharp
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Datadog.Trace;

namespace DatadogMauiApi.Framework.Filters
{
    public class DatadogSpanAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var span = GetDatadogSpan(actionContext);

            if (span != null)
            {
                actionContext.Request.Properties["datadog.span"] = span;

                var routeData = actionContext.RequestContext?.RouteData;
                if (routeData?.Route != null)
                {
                    var routeTemplate = routeData.Route.RouteTemplate;
                    if (!string.IsNullOrEmpty(routeTemplate))
                    {
                        span.SetTag("http.route", routeTemplate);
                    }
                }
            }

            base.OnActionExecuting(actionContext);
        }

        private ISpan GetDatadogSpan(HttpActionContext actionContext)
        {
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                return activeScope.Span;
            }

            if (actionContext.Request.Properties.ContainsKey("MS_OwinContext"))
            {
                var owinContext = actionContext.Request.Properties["MS_OwinContext"] as Microsoft.Owin.IOwinContext;
                if (owinContext != null && owinContext.Environment.ContainsKey("datadog.span"))
                {
                    return owinContext.Environment["datadog.span"] as ISpan;
                }
            }

            return null;
        }
    }
}
```

#### 2. Register the Filter Globally

**For Global.asax mode** - in `App_Start/WebApiConfig.cs`:
```csharp
public static void Register(HttpConfiguration config)
{
    // Register Datadog span filter globally
    config.Filters.Add(new Filters.DatadogSpanAttribute());

    // ... rest of config
}
```

**For OWIN mode** - in `Startup.cs`:
```csharp
public void Configuration(IAppBuilder app)
{
    var config = new HttpConfiguration();

    // Register Datadog span filter globally
    config.Filters.Add(new Filters.DatadogSpanAttribute());

    // Add OWIN middleware to capture parent span
    app.Use(async (context, next) =>
    {
        var scope = Datadog.Trace.Tracer.Instance.ActiveScope;

        if (scope != null)
        {
            // Store parent span in OWIN context
            context.Environment["datadog.span"] = scope.Span;

            // Add custom tags to parent span
            scope.Span.SetTag("custom.pipeline", "owin");
        }

        await next();
    });

    app.UseWebApi(config);
}
```

#### 3. Update Each Controller

**Before (direct tracer access):**
```csharp
public class AuthController : ApiController
{
    [HttpPost]
    [Route("login")]
    public IHttpActionResult Login([FromBody] LoginRequest request)
    {
        // Old approach - doesn't work reliably in OWIN
        var scope = Tracer.Instance.ActiveScope;
        if (scope != null)
        {
            scope.Span.SetTag("custom.operation.type", "user_login");
        }

        // ... rest of logic
    }
}
```

**After (using base controller):**
```csharp
public class AuthController : DatadogApiController  // Change: inherit from DatadogApiController
{
    [HttpPost]
    [Route("login")]
    public IHttpActionResult Login([FromBody] LoginRequest request)
    {
        // New approach - works in both pipelines
        var span = GetDatadogSpan();  // Change: use GetDatadogSpan()

        if (span != null)
        {
            span.ResourceName = "POST /auth/login";
            span.SetTag("custom.operation.type", "user_login");
        }

        // ... rest of logic
    }
}
```

#### 4. Add Files to .csproj

Ensure the new files are included in your project file:

```xml
<ItemGroup>
  <Compile Include="Controllers\DatadogApiController.cs" />
  <Compile Include="Filters\DatadogSpanAttribute.cs" />
  <!-- ... other files -->
</ItemGroup>
```

### Migration Checklist

- [ ] Create `Controllers/DatadogApiController.cs`
- [ ] Create `Filters/DatadogSpanAttribute.cs`
- [ ] Add files to `.csproj`
- [ ] Register filter in `WebApiConfig.cs` (Global.asax mode)
- [ ] Register filter and middleware in `Startup.cs` (OWIN mode)
- [ ] Update all controllers to inherit from `DatadogApiController`
- [ ] Replace `Tracer.Instance.ActiveScope` with `GetDatadogSpan()`
- [ ] Test in both pipeline modes
- [ ] Verify custom tags appear in Datadog UI

### Testing the Migration

1. **Build the project** - ensure no compilation errors
2. **Run in OWIN mode** - add `USE_OWIN` to compilation symbols
3. **Check debug output** - look for `[Datadog OWIN] Captured span` messages
4. **Make test requests** - verify spans are captured
5. **Check Datadog UI** - verify custom tags appear on `aspnet.request` spans
6. **Test Global.asax mode** - remove `USE_OWIN` and repeat tests
7. **Verify tags in both modes** - ensure consistent behavior

### Common Migration Issues

**Issue:** Controller doesn't recognize `GetDatadogSpan()` method
- **Fix:** Ensure controller inherits from `DatadogApiController`

**Issue:** Compilation error on `DatadogApiController` not found
- **Fix:** Add the file to `.csproj` and rebuild

**Issue:** Custom tags still not appearing
- **Fix:** Check that OWIN middleware is added BEFORE `app.UseWebApi(config)`
- **Fix:** Verify you're checking the parent `aspnet.request` span, not the child span

**Issue:** Filter not running
- **Fix:** Ensure filter is registered globally in both `WebApiConfig.cs` and `Startup.cs`

### Benefits After Migration

- ✅ Custom tags work in both Global.asax and OWIN modes
- ✅ Consistent controller code across pipeline modes
- ✅ Better performance with span caching
- ✅ Easier to scale to many controllers
- ✅ Single source of truth for span access logic

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
