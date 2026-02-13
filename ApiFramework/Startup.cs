using System;
using System.Web.Http;
using Microsoft.Owin;
using Owin;
using System.Web.Http.Cors;
using Microsoft.Owin.Extensions;
using Serilog;
using Serilog.Formatting.Compact;

[assembly: OwinStartup(typeof(DatadogMauiApi.Framework.Startup))]

namespace DatadogMauiApi.Framework
{
    /// <summary>
    /// OWIN Startup class for hosting Web API in OWIN pipeline
    ///
    /// To enable OWIN mode:
    /// 1. Add conditional compilation symbol "USE_OWIN" to project properties
    /// 2. Rebuild the project
    /// 3. OWIN pipeline will handle requests instead of Global.asax
    ///
    /// To disable OWIN and use Global.asax:
    /// 1. Remove "USE_OWIN" compilation symbol
    /// 2. Rebuild the project
    ///
    /// Both pipelines are configured identically for testing compatibility.
    /// </summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure Serilog for JSON file logging to enable log/trace correlation with Datadog
            // DD_LOGS_INJECTION will automatically inject trace IDs into log entries
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()  // Required for DD_LOGS_INJECTION
                .WriteTo.Console(new CompactJsonFormatter())  // JSON console logs
                .WriteTo.File(
                    new CompactJsonFormatter(),
                    path: @"C:\home\LogFiles\application.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1))
                .CreateLogger();

            System.Diagnostics.Debug.WriteLine("[Serilog] Logger configured for JSON file logging with Datadog correlation");
            // Create Web API configuration
            var config = new HttpConfiguration();

            // Register Datadog span filter globally (runs on every request)
            config.Filters.Add(new Filters.DatadogSpanAttribute());

            // Enable CORS (same as Global.asax config)
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);

            // Configure routes (same as WebApiConfig)
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // JSON formatter settings (same as Global.asax)
            var jsonFormatter = config.Formatters.JsonFormatter;
            jsonFormatter.SerializerSettings.ContractResolver =
                new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            jsonFormatter.SerializerSettings.NullValueHandling =
                Newtonsoft.Json.NullValueHandling.Ignore;

            // Remove XML formatter
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // Add Datadog diagnostic middleware - runs FIRST to check tracer status
            app.Use(async (context, next) =>
            {
                // Only log diagnostics on first request
                var diagnosticsRun = context.Environment.ContainsKey("datadog.diagnostics.run");

                if (!diagnosticsRun)
                {
                    context.Environment["datadog.diagnostics.run"] = true;

                    System.Diagnostics.Debug.WriteLine("========================================");
                    System.Diagnostics.Debug.WriteLine("[Datadog Diagnostics] Tracer Status");
                    System.Diagnostics.Debug.WriteLine("========================================");

                    // Check if Datadog Tracer is available
                    var tracer = Datadog.Trace.Tracer.Instance;
                    System.Diagnostics.Debug.WriteLine($"[Datadog] Tracer.Instance: {tracer != null}");

                    if (tracer != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Datadog] Tracer.DefaultServiceName: {tracer.DefaultServiceName ?? "(null)"}");

                        // Check tracer settings via reflection (if available)
                        try
                        {
                            var settingsType = tracer.GetType().GetProperty("Settings");
                            if (settingsType != null)
                            {
                                var settings = settingsType.GetValue(tracer);
                                if (settings != null)
                                {
                                    var envProp = settings.GetType().GetProperty("Environment");
                                    var serviceProp = settings.GetType().GetProperty("ServiceName");
                                    var enabledProp = settings.GetType().GetProperty("TraceEnabled");

                                    if (envProp != null)
                                        System.Diagnostics.Debug.WriteLine($"[Datadog] Settings.Environment: {envProp.GetValue(settings)}");
                                    if (serviceProp != null)
                                        System.Diagnostics.Debug.WriteLine($"[Datadog] Settings.ServiceName: {serviceProp.GetValue(settings)}");
                                    if (enabledProp != null)
                                        System.Diagnostics.Debug.WriteLine($"[Datadog] Settings.TraceEnabled: {enabledProp.GetValue(settings)}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Datadog] Could not read tracer settings: {ex.Message}");
                        }
                    }

                    // Check environment variables
                    System.Diagnostics.Debug.WriteLine("-------------------------------------------");
                    System.Diagnostics.Debug.WriteLine("[Datadog Diagnostics] Environment Variables");
                    System.Diagnostics.Debug.WriteLine("-------------------------------------------");

                    var envVars = new[]
                    {
                        "COR_ENABLE_PROFILING",
                        "COR_PROFILER",
                        "COR_PROFILER_PATH_64",
                        "CORECLR_ENABLE_PROFILING",
                        "CORECLR_PROFILER",
                        "DD_DOTNET_TRACER_HOME",
                        "DD_SERVICE",
                        "DD_ENV",
                        "DD_VERSION",
                        "DD_TRACE_ENABLED",
                        "DD_TRACE_DEBUG",
                        "DD_TRACE_STARTUP_LOGS"
                    };

                    foreach (var envVar in envVars)
                    {
                        var value = System.Environment.GetEnvironmentVariable(envVar);
                        if (!string.IsNullOrEmpty(value))
                        {
                            // Truncate long paths for readability
                            var displayValue = value.Length > 60 ? value.Substring(0, 57) + "..." : value;
                            System.Diagnostics.Debug.WriteLine($"[Datadog] {envVar} = {displayValue}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[Datadog] {envVar} = (not set)");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("========================================");
                }

                await next();
            });

            // Add Datadog span enrichment middleware - must run BEFORE UseWebApi
            // This ensures we capture the root aspnet.request span created by Datadog
            app.Use(async (context, next) =>
            {
                // Log request using Serilog (will include trace correlation via DD_LOGS_INJECTION)
                Log.Information("[OWIN Request] {Method} {Path}", context.Request.Method, context.Request.Path);

                // Get the active Datadog span (should be aspnet.request)
                var scope = Datadog.Trace.Tracer.Instance.ActiveScope;

                if (scope != null)
                {
                    // Store the span in the OWIN context so controllers can access it
                    context.Environment["datadog.span"] = scope.Span;

                    // Add request metadata to the root span
                    scope.Span.SetTag("http.method", context.Request.Method);
                    scope.Span.SetTag("http.url", context.Request.Uri.ToString());
                    scope.Span.SetTag("http.path", context.Request.Path.Value);

                    // Add custom tag to identify that this is the parent span
                    scope.Span.SetTag("custom.span.type", "aspnet.request.parent");
                    scope.Span.SetTag("custom.pipeline", "owin");

                    Log.Information("[Datadog Span] Captured span: {OperationName} SpanId={SpanId} TraceId={TraceId}",
                        scope.Span.OperationName, scope.Span.SpanId, scope.Span.TraceId);
                }
                else
                {
                    Log.Warning("[Datadog OWIN] No active span found - tracer may not be attached");
                }

                await next();

                // Add response status after processing
                if (scope != null)
                {
                    scope.Span.SetTag("http.status_code", context.Response.StatusCode.ToString());
                    Log.Information("[OWIN Response] {Method} {Path} Status={StatusCode}",
                        context.Request.Method, context.Request.Path, context.Response.StatusCode);

                    // After processing, check if there's still an active scope (might be the child span)
                    var currentScope = Datadog.Trace.Tracer.Instance.ActiveScope;
                    if (currentScope != null && currentScope.Span.SpanId != scope.Span.SpanId)
                    {
                        Log.Debug("[Datadog OWIN] Active span changed to: {OperationName} SpanId={SpanId}",
                            currentScope.Span.OperationName, currentScope.Span.SpanId);
                    }
                }
            });

            // Add custom OWIN middleware for logging/debugging
            app.Use(async (context, next) =>
            {
                Log.Debug("[OWIN Pipeline] {Method} {Path}", context.Request.Method, context.Request.Path);
                await next();
                Log.Debug("[OWIN Pipeline] Response: {StatusCode}", context.Response.StatusCode);
            });

            // Use Web API with OWIN
            app.UseWebApi(config);

            Log.Information("[OWIN] Startup configured - OWIN pipeline is active");
            System.Diagnostics.Debug.WriteLine("[OWIN] Startup configured - OWIN pipeline is active");
        }
    }
}
