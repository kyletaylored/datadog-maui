using Newtonsoft.Json;
using System;

namespace DatadogMauiApi.Framework.Models
{
    public class UserProfile
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("fullName")]
        public string FullName { get; set; }

        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("lastLoginAt")]
        public DateTime? LastLoginAt { get; set; }
    }
}
