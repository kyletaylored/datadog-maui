using System;

namespace DatadogMauiApi.Framework.Models
{
    public class DataSubmission
    {
        public string CorrelationId { get; set; }
        public string SessionName { get; set; }
        public string Notes { get; set; }
        public decimal NumericValue { get; set; }
    }
}
