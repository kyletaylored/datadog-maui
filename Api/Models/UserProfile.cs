namespace DatadogMauiApi.Models;

public record UserProfile(
    string UserId,
    string Username,
    string Email,
    string FullName,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
