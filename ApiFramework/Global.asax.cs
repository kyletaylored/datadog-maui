using System;
using System.Web;
using System.Web.Http;

namespace DatadogMauiApi.Framework
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            // Log to Datadog - the Datadog HTTP module will automatically capture this
            System.Diagnostics.Trace.TraceError("Unhandled exception: {0}", exception);
        }
    }
}
