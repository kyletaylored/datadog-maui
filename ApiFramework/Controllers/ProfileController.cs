using System.Linq;
using System.Web.Http;
using DatadogMauiApi.Framework.Models;
using DatadogMauiApi.Framework.Services;

namespace DatadogMauiApi.Framework.Controllers
{
    [RoutePrefix("")]
    public class ProfileController : ApiController
    {
        private readonly SessionManager _sessionManager;

        public ProfileController()
        {
            _sessionManager = new SessionManager();
        }

        [HttpGet]
        [Route("profile")]
        public IHttpActionResult GetProfile()
        {
            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || string.IsNullOrEmpty(authHeader.Parameter))
            {
                return Unauthorized();
            }

            var validation = _sessionManager.ValidateSession(authHeader.Parameter);
            if (!validation.Item1 || validation.Item2 == null)
            {
                return Unauthorized();
            }

            var profile = _sessionManager.GetUserProfile(validation.Item2);
            if (profile == null)
            {
                return NotFound();
            }

            return Ok(profile);
        }

        [HttpPut]
        [Route("profile")]
        public IHttpActionResult UpdateProfile([FromBody] UserProfile updatedProfile)
        {
            if (updatedProfile == null)
            {
                return BadRequest("Profile data is required");
            }

            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || string.IsNullOrEmpty(authHeader.Parameter))
            {
                return Unauthorized();
            }

            var validation = _sessionManager.ValidateSession(authHeader.Parameter);
            if (!validation.Item1 || validation.Item2 == null)
            {
                return Unauthorized();
            }

            // Ensure user can only update their own profile
            if (validation.Item2 != updatedProfile.UserId)
            {
                return StatusCode(System.Net.HttpStatusCode.Forbidden);
            }

            var success = _sessionManager.UpdateUserProfile(validation.Item2, updatedProfile.FullName, updatedProfile.Email);

            if (success)
            {
                return Ok(new { message = "Profile updated successfully" });
            }

            return Content(System.Net.HttpStatusCode.BadRequest, new { message = "Profile update failed" });
        }
    }
}
