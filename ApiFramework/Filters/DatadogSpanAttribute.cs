using Datadog.Trace;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace DatadogMauiApi.Framework.Filters
{
    /// <summary>
    /// Action filter that captures the Datadog span once per request and stores it in the request properties
    /// This is more efficient than looking up the span in every controller method
    /// </summary>
    public class DatadogSpanAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            // Get the Datadog span once at the start of the request
            var span = GetDatadogSpan(actionContext);

            System.Diagnostics.Debug.WriteLine($"[DatadogSpanAttribute] Filter executing for {actionContext.Request.Method} {actionContext.Request.RequestUri?.PathAndQuery}");
            System.Diagnostics.Debug.WriteLine($"[DatadogSpanAttribute] Span found: {span != null}, OperationName: {span?.OperationName}");

            if (span != null)
            {
                // Store it in request properties so controllers can easily access it
                actionContext.Request.Properties["datadog.span"] = span;
                System.Diagnostics.Debug.WriteLine($"[DatadogSpanAttribute] Cached span in request properties");

                // Optionally set default resource name based on route
                var routeData = actionContext.RequestContext?.RouteData;
                if (routeData?.Route != null)
                {
                    var routeTemplate = routeData.Route.RouteTemplate;
                    if (!string.IsNullOrEmpty(routeTemplate))
                    {
                        span.SetTag("http.route", routeTemplate);
                        System.Diagnostics.Debug.WriteLine($"[DatadogSpanAttribute] Set http.route tag: {routeTemplate}");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DatadogSpanAttribute] WARNING: No span found!");
            }

            base.OnActionExecuting(actionContext);
        }

        private ISpan GetDatadogSpan(HttpActionContext actionContext)
        {
            // Try to get the active span from the tracer
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                return activeScope.Span;
            }

            // Fallback for OWIN mode: Check if span was stored in OWIN context by middleware
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
