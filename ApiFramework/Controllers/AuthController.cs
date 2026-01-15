using System.Linq;
using System.Web.Http;
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
            _sessionManager = new SessionManager();
        }

        [HttpPost]
        [Route("login")]
        public IHttpActionResult Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest("Username and password are required");
            }

            var response = _sessionManager.AuthenticateUser(request.Username, request.Password);

            if (response.Success)
            {
                return Ok(response);
            }

            return Unauthorized();
        }

        [HttpPost]
        [Route("logout")]
        public IHttpActionResult Logout()
        {
            var authHeader = Request.Headers.Authorization;
            if (authHeader == null || string.IsNullOrEmpty(authHeader.Parameter))
            {
                return Content(System.Net.HttpStatusCode.BadRequest, new { message = "No token provided" });
            }

            var success = _sessionManager.Logout(authHeader.Parameter);

            if (success)
            {
                return Ok(new { message = "Logged out successfully" });
            }

            return Content(System.Net.HttpStatusCode.BadRequest, new { message = "Logout failed" });
        }
    }
}
