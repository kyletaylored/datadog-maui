using System.Web.Http;
using Microsoft.Owin;
using Owin;
using System.Web.Http.Cors;

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
