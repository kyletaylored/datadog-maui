using System.Web.Http;
using System.Web.Http.Cors;

namespace DatadogMauiApi.Framework
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Register Datadog span filter globally (runs on every request)
            config.Filters.Add(new Filters.DatadogSpanAttribute());

            // Enable CORS for all origins (configure as needed for production)
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);

            // Web API configuration and services
            config.Formatters.JsonFormatter.SerializerSettings.ReferenceLoopHandling =
                Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling =
                Newtonsoft.Json.NullValueHandling.Ignore;

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
