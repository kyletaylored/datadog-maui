using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http;

namespace DatadogMauiApi.Framework.Controllers
{
    [RoutePrefix("")]
    public class RumConfigController : ApiController
    {
        [HttpGet]
        [Route("rum-config.js")]
        public HttpResponseMessage GetRumConfig()
        {
            // Check if RUM is enabled via appSettings (Web.config) or environment variable (defaults to true)
            // Priority: Web.config appSettings > Environment Variable
            var rumEnabled = ConfigurationManager.AppSettings["DD_RUM_ENABLED"]
                             ?? Environment.GetEnvironmentVariable("DD_RUM_ENABLED");

            var isRumEnabled = string.IsNullOrEmpty(rumEnabled) ||
                               rumEnabled.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                               rumEnabled.Equals("1", StringComparison.OrdinalIgnoreCase);

            string jsConfig;

            if (!isRumEnabled)
            {
                // RUM is disabled - return empty/no-op config
                jsConfig = @"// Datadog RUM is disabled (DD_RUM_ENABLED=false)
console.info('[Datadog] RUM tracking is disabled');
window.DD_RUM_CONFIG = {
    enabled: false
};";
            }
            else
            {
                // Generate RUM config dynamically from Web.config appSettings
                var clientToken = ConfigurationManager.AppSettings["DD_RUM_WEB_CLIENT_TOKEN"] ?? "";
                var applicationId = ConfigurationManager.AppSettings["DD_RUM_WEB_APPLICATION_ID"] ?? "";
                var site = ConfigurationManager.AppSettings["DD_SITE"] ?? "datadoghq.com";
                var service = ConfigurationManager.AppSettings["DD_RUM_WEB_SERVICE"] ?? "datadog-maui-web-framework";
                var env = ConfigurationManager.AppSettings["DD_ENV"] ?? "local";
                var version = ConfigurationManager.AppSettings["DD_VERSION"] ?? "1.0.0";

                jsConfig = $@"// Datadog RUM Configuration (generated dynamically)
console.log('[Datadog] RUM config loaded from Web.config');
window.DD_RUM_CONFIG = {{
    enabled: true,
    clientToken: '{clientToken}',
    applicationId: '{applicationId}',
    site: '{site}',
    service: '{service}',
    env: '{env}',
    version: '{version}',
    sessionSampleRate: 100,
    sessionReplaySampleRate: 100,
    trackBfcacheViews: true,
    defaultPrivacyLevel: 'mask-user-input',
    allowedTracingUrls: ['localhost', '127.0.0.1', (url) => {{ return true; }}]
}};";
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(jsConfig, Encoding.UTF8, "application/javascript");
            return response;
        }
    }
}
