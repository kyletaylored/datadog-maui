using Newtonsoft.Json;
using System.Collections.Generic;

namespace DatadogMauiApi.Framework.Models
{
    public class ConfigResponse
    {
        [JsonProperty("webViewUrl")]
        public string WebViewUrl { get; set; }

        [JsonProperty("featureFlags")]
        public Dictionary<string, bool> FeatureFlags { get; set; }
    }
}
