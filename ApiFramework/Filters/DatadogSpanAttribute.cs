using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Http.Routing;
using Datadog.Trace;

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

            if (span != null)
            {
                // Store it in request properties so controllers can easily access it
                actionContext.Request.Properties["datadog.span"] = span;

                // Optionally set default resource name based on route
                var routeTemplate = actionContext.Request.GetRouteData()?.Route?.RouteTemplate;
                if (!string.IsNullOrEmpty(routeTemplate))
                {
                    span.SetTag("http.route", routeTemplate);
                }
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
