using System;
using Newtonsoft.Json;

namespace DatadogMauiApi.Framework.Models
{
    public class DataSubmission
    {
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }

        [JsonProperty("sessionName")]
        public string SessionName { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }

        [JsonProperty("numericValue")]
        public decimal NumericValue { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }
    }
}
