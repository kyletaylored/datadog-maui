using Datadog.Trace;
using DatadogMauiApi.Framework.Services;
using System;
using System.Web.Http;

namespace DatadogMauiApi.Framework.Controllers
{
    [RoutePrefix("health")]
    public class HealthController : ApiController
    {
        private readonly SessionManager _sessionManager;

        public HealthController()
        {
            _sessionManager = SessionManager.Instance;
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetHealth()
        {
            // Get the active span created by automatic instrumentation
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                activeScope.Span.ResourceName = "GET /health";
                activeScope.Span.SetTag("custom.operation.type", "health_check");

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
            }

            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}
