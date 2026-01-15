using Newtonsoft.Json;

namespace DatadogMauiApi.Framework.Models
{
    public class LoginResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
