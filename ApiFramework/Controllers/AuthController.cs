using System.Linq;
using System.Web.Http;
using Datadog.Trace;
using DatadogMauiApi.Framework.Models;
using DatadogMauiApi.Framework.Services;

namespace DatadogMauiApi.Framework.Controllers
{
    [RoutePrefix("auth")]
    public class AuthController : ApiController
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
            // Get the active span created by automatic instrumentation
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                activeScope.Span.ResourceName = "POST /auth/login";
                activeScope.Span.SetTag("custom.operation.type", "user_login");
            }

            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required");
            }

            if (activeScope != null)
            {
                activeScope.Span.SetTag("custom.auth.username", request.Username);
            }

            var response = _sessionManager.AuthenticateUser(request.Username, request.Password);

            if (response.Success)
            {
                if (activeScope != null)
                {
                    activeScope.Span.SetTag("custom.auth.success", "true");
                    activeScope.Span.SetTag("custom.user.id", response.UserId);
                }
                return Ok(response);
            }

            if (activeScope != null)
            {
                activeScope.Span.SetTag("custom.auth.success", "false");
            }
            return Unauthorized();
        }

        [HttpPost]
        [Route("logout")]
        public IHttpActionResult Logout()
        {
            // Get the active span created by automatic instrumentation
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                activeScope.Span.ResourceName = "POST /auth/logout";
                activeScope.Span.SetTag("custom.operation.type", "user_logout");
            }

            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || string.IsNullOrEmpty(authHeader.Parameter))
            {
                if (activeScope != null)
                {
                    activeScope.Span.SetTag("custom.auth.present", "false");
                }
                return Content(System.Net.HttpStatusCode.BadRequest, new { message = "No token provided" });
            }

            var success = _sessionManager.Logout(authHeader.Parameter);

            if (success)
            {
                if (activeScope != null)
                {
                    activeScope.Span.SetTag("custom.logout.success", "true");
                }
                return Ok(new { message = "Logged out successfully" });
            }

            if (activeScope != null)
            {
                activeScope.Span.SetTag("custom.logout.success", "false");
            }
            return Content(System.Net.HttpStatusCode.BadRequest, new { message = "Logout failed" });
        }
    }
}
