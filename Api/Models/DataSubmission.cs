namespace DatadogMauiApi.Models;

public record DataSubmission(
    string CorrelationId,
    string SessionName,
    string Notes,
    decimal NumericValue
);
