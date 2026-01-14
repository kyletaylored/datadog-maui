namespace DatadogMauiApi.Models;

public record LoginResponse(
    bool Success,
    string? Token,
    string? Username,
    string? UserId,
    string? Message
);
