using System.Web.Http;
using Microsoft.Owin;
using Owin;
using System.Web.Http.Cors;
using Microsoft.Owin.Extensions;

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
                routeTemplate: "{controller}/{action}/{id}",
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

            // Add Datadog span enrichment middleware - must run BEFORE UseWebApi
            // This ensures we capture the root aspnet.request span created by Datadog
            app.Use(async (context, next) =>
            {
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

                    System.Diagnostics.Debug.WriteLine($"[Datadog OWIN] Captured span: {scope.Span.OperationName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[Datadog OWIN] WARNING: No active span found");
                }

                await next();

                // Add response status after processing
                if (scope != null)
                {
                    scope.Span.SetTag("http.status_code", context.Response.StatusCode.ToString());
                }
            });

            // Add custom OWIN middleware for logging/debugging
            app.Use(async (context, next) =>
            {
                System.Diagnostics.Debug.WriteLine($"[OWIN] {context.Request.Method} {context.Request.Path}");
                await next();
                System.Diagnostics.Debug.WriteLine($"[OWIN] Response: {context.Response.StatusCode}");
            });

            // Use Web API with OWIN
            app.UseWebApi(config);

            System.Diagnostics.Debug.WriteLine("[OWIN] Startup configured - OWIN pipeline is active");
        }
    }
}
