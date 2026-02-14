using Datadog.Trace;
using DatadogMauiApi.Framework.Models;
using DatadogMauiApi.Framework.Services;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace DatadogMauiApi.Framework.Controllers
{
    [RoutePrefix("")]
    public class ConfigController : ApiController
    {
        private readonly SessionManager _sessionManager;

        public ConfigController()
        {
            _sessionManager = SessionManager.Instance;
        }

        [HttpGet]
        [Route("config")]
        public IHttpActionResult GetConfig()
        {
            // Get the active span created by automatic instrumentation
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                activeScope.Span.ResourceName = "GET /config";
                activeScope.Span.SetTag("custom.operation.type", "config_fetch");

                // Extract correlation ID from headers if present
                IEnumerable<string> correlationIds;
                if (Request.Headers.TryGetValues("X-Correlation-ID", out correlationIds))
                {
                    var correlationId = correlationIds.FirstOrDefault();
                    if (!string.IsNullOrEmpty(correlationId))
                    {
                        activeScope.Span.SetTag("custom.correlation.id", correlationId);
                    }
                }

                // Check if user is authenticated
                var authHeader = Request.Headers.Authorization;
                if (authHeader != null && !string.IsNullOrEmpty(authHeader.Parameter))
                {
                    var validation = _sessionManager.ValidateSession(authHeader.Parameter);
                    if (validation.Item1 && validation.Item2 != null)
                    {
                        activeScope.Span.SetTag("custom.user.id", validation.Item2);
                        activeScope.Span.SetTag("custom.authenticated", "true");
                    }
                    else
                    {
                        activeScope.Span.SetTag("custom.authenticated", "false");
                    }
                }
                else
                {
                    activeScope.Span.SetTag("custom.authenticated", "false");
                }

                var config = new ConfigResponse
                {
                    WebViewUrl = "http://10.0.2.2:5000",
                    FeatureFlags = new Dictionary<string, bool>
                    {
                        { "EnableTelemetry", true },
                        { "EnableAdvancedFeatures", false }
                    }
                };

                activeScope.Span.SetTag("custom.config.webview_url", config.WebViewUrl);
                activeScope.Span.SetTag("custom.config.feature_flags_count", config.FeatureFlags.Count.ToString());

                return Ok(config);
            }

            // Fallback if no active span
            return Ok(new ConfigResponse
            {
                WebViewUrl = "http://10.0.2.2:5000",
                FeatureFlags = new Dictionary<string, bool>
                {
                    { "EnableTelemetry", true },
                    { "EnableAdvancedFeatures", false }
                }
            });
        }
    }
}
