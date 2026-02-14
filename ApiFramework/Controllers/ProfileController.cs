using Datadog.Trace;
using DatadogMauiApi.Framework.Models;
using DatadogMauiApi.Framework.Services;
using System.Web.Http;

namespace DatadogMauiApi.Framework.Controllers
{
    [RoutePrefix("")]
    public class ProfileController : ApiController
    {
        private readonly SessionManager _sessionManager;

        public ProfileController()
        {
            _sessionManager = SessionManager.Instance;
        }

        [HttpGet]
        [Route("profile")]
        public IHttpActionResult GetProfile()
        {
            // Get the active span created by automatic instrumentation
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                activeScope.Span.ResourceName = "GET /profile";
                activeScope.Span.SetTag("custom.operation.type", "get_profile");
            }

            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || string.IsNullOrEmpty(authHeader.Parameter))
            {
                if (activeScope != null)
                {
                    activeScope.Span.SetTag("custom.auth.present", "false");
                }
                return Unauthorized();
            }

            var validation = _sessionManager.ValidateSession(authHeader.Parameter);
            if (!validation.Item1 || validation.Item2 == null)
            {
                if (activeScope != null)
                {
                    activeScope.Span.SetTag("custom.session.valid", "false");
                }
                return Unauthorized();
            }

            if (activeScope != null)
            {
                activeScope.Span.SetTag("custom.session.valid", "true");
                activeScope.Span.SetTag("custom.user.id", validation.Item2);
            }

            var profile = _sessionManager.GetUserProfile(validation.Item2);
            if (profile == null)
            {
                if (activeScope != null)
                {
                    activeScope.Span.SetTag("custom.profile.found", "false");
                }
                return NotFound();
            }

            if (activeScope != null)
            {
                activeScope.Span.SetTag("custom.profile.found", "true");
                activeScope.Span.SetTag("custom.user.username", profile.Username);
            }

            return Ok(profile);
        }

        [HttpPut]
        [Route("profile")]
        public IHttpActionResult UpdateProfile([FromBody] UserProfile updatedProfile)
        {
            // Get the active span created by automatic instrumentation
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                activeScope.Span.ResourceName = "PUT /profile";
                activeScope.Span.SetTag("custom.operation.type", "update_profile");
            }

            if (updatedProfile == null)
            {
                return BadRequest("Profile data is required");
            }

            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || string.IsNullOrEmpty(authHeader.Parameter))
            {
                if (activeScope != null)
                {
                    activeScope.Span.SetTag("custom.auth.present", "false");
                }
                return Unauthorized();
            }

            var validation = _sessionManager.ValidateSession(authHeader.Parameter);
            if (!validation.Item1 || validation.Item2 == null)
            {
                if (activeScope != null)
                {
                    activeScope.Span.SetTag("custom.session.valid", "false");
                }
                return Unauthorized();
            }

            if (activeScope != null)
            {
                activeScope.Span.SetTag("custom.session.valid", "true");
                activeScope.Span.SetTag("custom.user.id", validation.Item2);
            }

            // Ensure user can only update their own profile
            if (validation.Item2 != updatedProfile.UserId)
            {
                if (activeScope != null)
                {
                    activeScope.Span.SetTag("custom.update.authorized", "false");
                }
                return StatusCode(System.Net.HttpStatusCode.Forbidden);
            }

            if (activeScope != null)
            {
                activeScope.Span.SetTag("custom.update.authorized", "true");
                activeScope.Span.SetTag("custom.update.fields", "fullName,email");
            }

            var success = _sessionManager.UpdateUserProfile(validation.Item2, updatedProfile.FullName, updatedProfile.Email);

            if (success)
            {
                if (activeScope != null)
                {
                    activeScope.Span.SetTag("custom.update.success", "true");
                }
                return Ok(new { message = "Profile updated successfully" });
            }

            if (activeScope != null)
            {
                activeScope.Span.SetTag("custom.update.success", "false");
            }
            return Content(System.Net.HttpStatusCode.BadRequest, new { message = "Profile update failed" });
        }
    }
}
