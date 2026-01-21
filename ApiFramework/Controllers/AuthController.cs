using System.Linq;
using System.Web.Http;
using Datadog.Trace;
using DatadogMauiApi.Framework.Models;
using DatadogMauiApi.Framework.Services;

namespace DatadogMauiApi.Framework.Controllers
{
    [RoutePrefix("auth")]
    public class AuthController : DatadogApiController
    {
        private readonly SessionManager _sessionManager;

        public AuthController()
        {
            _sessionManager = SessionManager.Instance;
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login([FromBody] LoginRequest request)
        {
            // Get the active Datadog span (works in both Global.asax and OWIN modes)
            var span = GetDatadogSpan();

            System.Diagnostics.Debug.WriteLine($"[AuthController.Login] GetDatadogSpan returned: {span != null}");
            if (span != null)
            {
                System.Diagnostics.Debug.WriteLine($"[AuthController.Login] Span OperationName: {span.OperationName}, ResourceName: {span.ResourceName}");
                span.ResourceName = "POST /auth/login";
                span.SetTag("custom.operation.type", "user_login");
                span.SetTag("custom.pipeline", "owin"); // Tag to identify OWIN mode
                System.Diagnostics.Debug.WriteLine($"[AuthController.Login] Set custom tags on span");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[AuthController.Login] WARNING: Span is null, cannot set custom tags!");
            }

            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required");
            }

            if (span != null)
            {
                span.SetTag("custom.auth.username", request.Username);
            }

            var response = _sessionManager.AuthenticateUser(request.Username, request.Password);

            if (response.Success)
            {
                if (span != null)
                {
                    span.SetTag("custom.auth.success", "true");
                    span.SetTag("custom.user.id", response.UserId);
                }
                return Ok(response);
            }

            if (span != null)
            {
                span.SetTag("custom.auth.success", "false");
            }
            return Unauthorized();
        }

        [HttpPost]
        [Route("logout")]
        public IHttpActionResult Logout()
        {
            // Get the active Datadog span (works in both Global.asax and OWIN modes)
            var span = GetDatadogSpan();

            if (span != null)
            {
                span.ResourceName = "POST /auth/logout";
                span.SetTag("custom.operation.type", "user_logout");
                span.SetTag("custom.pipeline", "owin");
            }

            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || string.IsNullOrEmpty(authHeader.Parameter))
            {
                if (span != null)
                {
                    span.SetTag("custom.auth.present", "false");
                }
                return Content(System.Net.HttpStatusCode.BadRequest, new { message = "No token provided" });
            }

            var success = _sessionManager.Logout(authHeader.Parameter);

            if (success)
            {
                if (span != null)
                {
                    span.SetTag("custom.logout.success", "true");
                }
                return Ok(new { message = "Logged out successfully" });
            }

            if (span != null)
            {
                span.SetTag("custom.logout.success", "false");
            }
            return Content(System.Net.HttpStatusCode.BadRequest, new { message = "Logout failed" });
        }
    }
}
