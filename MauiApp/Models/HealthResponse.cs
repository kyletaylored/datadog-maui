namespace DatadogMauiApp.Models;

public record HealthResponse(
    string Status,
    string Environment,
    DateTime Timestamp,
    string Uptime
);
