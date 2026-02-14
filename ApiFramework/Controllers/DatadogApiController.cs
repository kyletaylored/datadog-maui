using Datadog.Trace;
using System.Web.Http;

namespace DatadogMauiApi.Framework.Controllers
{
    /// <summary>
    /// Base controller that provides Datadog span access for both Global.asax and OWIN pipelines
    /// </summary>
    public abstract class DatadogApiController : ApiController
    {
        /// <summary>
        /// Gets the current Datadog span from request properties (cached by DatadogSpanAttribute filter)
        /// or falls back to looking it up from the tracer/OWIN context
        /// Works in both Global.asax and OWIN pipeline modes
        /// </summary>
        /// <returns>The active Datadog span, or null if not found</returns>
        protected ISpan GetDatadogSpan()
        {
            // First, try to get the cached span from request properties
            // (set by DatadogSpanAttribute filter - most efficient)
            if (Request.Properties.ContainsKey("datadog.span"))
            {
                return Request.Properties["datadog.span"] as ISpan;
            }

            // Fallback: Try to get the active span from the tracer
            // This should work in both pipelines
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                return activeScope.Span;
            }

            // Fallback for OWIN mode: Check if span was stored in OWIN context by middleware
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
