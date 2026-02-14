using System.Web;
using System.Web.Http;

namespace DatadogMauiApi.Framework
{
    /// <summary>
    /// Global.asax application class
    ///
    /// This class handles ASP.NET pipeline events when NOT using OWIN.
    /// When USE_OWIN is defined, the OWIN Startup class handles Web API configuration instead.
    ///
    /// To switch between pipelines:
    /// - OWIN: Add "USE_OWIN" to Project Properties → Build → Conditional compilation symbols
    /// - Global.asax: Remove "USE_OWIN" compilation symbol
    /// </summary>
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
#if USE_OWIN
            System.Diagnostics.Debug.WriteLine("[Global.asax] USE_OWIN is defined - Skipping Web API config (handled by OWIN Startup)");
            // When using OWIN, the Startup class configures Web API
            // Global.asax is still used for application-level events but not for Web API routing
#else
            System.Diagnostics.Debug.WriteLine("[Global.asax] Configuring Web API via traditional ASP.NET pipeline");
            GlobalConfiguration.Configure(WebApiConfig.Register);
#endif
        }

        protected void Application_BeginRequest()
        {
#if USE_OWIN
            System.Diagnostics.Debug.WriteLine($"[Global.asax/OWIN Mode] BeginRequest: {Request.HttpMethod} {Request.Url.PathAndQuery}");
#else
            System.Diagnostics.Debug.WriteLine($"[Global.asax] BeginRequest: {Request.HttpMethod} {Request.Url.PathAndQuery}");
#endif
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            // Log to Datadog - the Datadog tracer will automatically capture this
            System.Diagnostics.Trace.TraceError("Unhandled exception: {0}", exception);

#if USE_OWIN
            System.Diagnostics.Debug.WriteLine("[Global.asax/OWIN Mode] Application_Error triggered");
#else
            System.Diagnostics.Debug.WriteLine("[Global.asax] Application_Error triggered");
#endif
        }
    }
}
