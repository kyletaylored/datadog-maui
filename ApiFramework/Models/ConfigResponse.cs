using System.Collections.Generic;

namespace DatadogMauiApi.Framework.Models
{
    public class ConfigResponse
    {
        public string WebViewUrl { get; set; }
        public Dictionary<string, bool> FeatureFlags { get; set; }
    }
}
