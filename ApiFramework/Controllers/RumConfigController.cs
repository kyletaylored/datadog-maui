using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Text;
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
            // Read from Web.config appSettings
            var clientToken = ConfigurationManager.AppSettings["DD_RUM_CLIENT_TOKEN"] ?? "";
            var applicationId = ConfigurationManager.AppSettings["DD_RUM_APPLICATION_ID"] ?? "";
            var site = ConfigurationManager.AppSettings["DD_SITE"] ?? "datadoghq.com";
            var env = ConfigurationManager.AppSettings["DD_ENV"] ?? "local";
            var service = ConfigurationManager.AppSettings["DD_SERVICE"] ?? "datadog-maui-web-framework";

            // Generate JavaScript configuration
            var jsConfig = $@"// Datadog RUM Configuration (auto-generated)
window.DD_RUM_CONFIG = {{
    clientToken: '{clientToken}',
    applicationId: '{applicationId}',
    site: '{site}',
    service: '{service}',
    env: '{env}',
    version: '1.0.0',
    sessionSampleRate: 100,
    sessionReplaySampleRate: 100,
    trackBfcacheViews: true,
    defaultPrivacyLevel: 'mask-user-input',
    allowedTracingUrls: [
        'localhost',
        '127.0.0.1',
        window.location.host
    ]
}};

console.log('[Datadog] RUM configuration loaded');
";

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(jsConfig, Encoding.UTF8, "application/javascript");
            return response;
        }
    }
}
