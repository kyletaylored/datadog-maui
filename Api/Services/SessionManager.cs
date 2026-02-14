using Datadog.Trace;
using DatadogMauiApi.Models;

namespace DatadogMauiApi.Services;

public class SessionManager
{
    private readonly ILogger<SessionManager> _logger;

    // Simple in-memory user store (in production, use a real database)
    private static readonly Dictionary<string, UserProfile> _users = new()
    {
        ["demo"] = new UserProfile(
            UserId: "user-001",
            Username: "demo",
            Email: "demo@example.com",
            FullName: "Demo User",
            CreatedAt: DateTime.UtcNow.AddDays(-30),
            LastLoginAt: null
        ),
        ["admin"] = new UserProfile(
            UserId: "user-002",
            Username: "admin",
            Email: "admin@example.com",
            FullName: "Admin User",
            CreatedAt: DateTime.UtcNow.AddDays(-60),
            LastLoginAt: null
        ),
        ["test"] = new UserProfile(
            UserId: "user-003",
            Username: "test",
            Email: "test@example.com",
            FullName: "Test User",
            CreatedAt: DateTime.UtcNow.AddDays(-15),
            LastLoginAt: null
        )
    };

    // Active sessions
    private static readonly Dictionary<string, (string UserId, DateTime ExpiresAt)> _sessions = new();

    public SessionManager(ILogger<SessionManager> logger)
    {
        _logger = logger;
    }

    public LoginResponse AuthenticateUser(string username, string password)
    {
        using var scope = Tracer.Instance.StartActive("auth.login");
        scope.Span.ResourceName = "SessionManager.AuthenticateUser";
        scope.Span.SetTag("auth.username", username);
        scope.Span.SetTag("auth.method", "password");
        scope.Span.SetTag("service.operation", "user_login");

        _logger.LogInformation("[Auth] Login attempt for user: {Username}", username);

        // Simple password check (in production, use proper password hashing)
        if (!_users.ContainsKey(username) || password != "password")
        {
            _logger.LogWarning("[Auth] Failed login attempt for user: {Username}", username);

            scope.Span.SetTag("auth.success", "false");
            scope.Span.SetTag("auth.failure_reason", "invalid_credentials");

            return new LoginResponse(
                Success: false,
                Token: null,
                Username: null,
                UserId: null,
                Message: "Invalid username or password"
            );
        }

        var user = _users[username];
        var token = GenerateToken(user.UserId);

        // Store session
        _sessions[token] = (user.UserId, DateTime.UtcNow.AddHours(24));

        // Update last login
        _users[username] = user with { LastLoginAt = DateTime.UtcNow };

        _logger.LogInformation("[Auth] Successful login for user: {Username}, UserId: {UserId}", username, user.UserId);

        scope.Span.SetTag("auth.success", "true");
        scope.Span.SetTag("user.id", user.UserId);
        scope.Span.SetTag("user.username", username);
        scope.Span.SetTag("user.email", user.Email);

        return new LoginResponse(
            Success: true,
            Token: token,
            Username: user.Username,
            UserId: user.UserId,
            Message: "Login successful"
        );
    }

    public (bool IsValid, string? UserId) ValidateSession(string token)
    {
        using var scope = Tracer.Instance.StartActive("auth.validate_session");
        scope.Span.ResourceName = "SessionManager.ValidateSession";
        scope.Span.SetTag("auth.token_length", token?.Length ?? 0);

        if (string.IsNullOrEmpty(token) || !_sessions.ContainsKey(token))
        {
            _logger.LogWarning("[Auth] Invalid or missing session token");

            scope.Span.SetTag("session.valid", "false");
            scope.Span.SetTag("session.failure_reason", "token_not_found");

            return (false, null);
        }

        var (userId, expiresAt) = _sessions[token];

        if (DateTime.UtcNow > expiresAt)
        {
            _logger.LogWarning("[Auth] Expired session for userId: {UserId}", userId);
            _sessions.Remove(token);

            scope.Span.SetTag("session.valid", "false");
            scope.Span.SetTag("session.failure_reason", "token_expired");
            scope.Span.SetTag("user.id", userId);

            return (false, null);
        }

        _logger.LogDebug("[Auth] Valid session for userId: {UserId}", userId);

        scope.Span.SetTag("session.valid", "true");
        scope.Span.SetTag("user.id", userId);

        return (true, userId);
    }

    public UserProfile? GetUserProfile(string userId)
    {
        using var scope = Tracer.Instance.StartActive("user.get_profile");
        scope.Span.ResourceName = "SessionManager.GetUserProfile";
        scope.Span.SetTag("user.id", userId);
        scope.Span.SetTag("operation.type", "profile_fetch");

        var user = _users.Values.FirstOrDefault(u => u.UserId == userId);

        if (user == null)
        {
            _logger.LogWarning("[Profile] User not found: {UserId}", userId);

            scope.Span.SetTag("profile.found", "false");

            return null;
        }

        _logger.LogInformation("[Profile] Retrieved profile for user: {UserId}, Username: {Username}", userId, user.Username);

        scope.Span.SetTag("profile.found", "true");
        scope.Span.SetTag("user.username", user.Username);
        scope.Span.SetTag("user.email", user.Email);

        return user;
    }

    public bool UpdateUserProfile(string userId, string fullName, string email)
    {
        using var scope = Tracer.Instance.StartActive("user.update_profile");
        scope.Span.ResourceName = "SessionManager.UpdateUserProfile";
        scope.Span.SetTag("user.id", userId);
        scope.Span.SetTag("operation.type", "profile_update");
        scope.Span.SetTag("update.fields", "fullName,email");

        var user = _users.Values.FirstOrDefault(u => u.UserId == userId);

        if (user == null)
        {
            _logger.LogWarning("[Profile] Cannot update - user not found: {UserId}", userId);

            scope.Span.SetTag("update.success", "false");
            scope.Span.SetTag("update.failure_reason", "user_not_found");

            return false;
        }

        // Update the user profile
        var updatedUser = user with { FullName = fullName, Email = email };
        _users[user.Username] = updatedUser;

        _logger.LogInformation("[Profile] Updated profile for user: {UserId}, FullName: {FullName}, Email: {Email}",
            userId, fullName, email);

        scope.Span.SetTag("update.success", "true");
        scope.Span.SetTag("user.username", user.Username);
        scope.Span.SetTag("user.email", email);

        return true;
    }

    public bool Logout(string token)
    {
        using var scope = Tracer.Instance.StartActive("auth.logout");
        scope.Span.ResourceName = "SessionManager.Logout";

        if (string.IsNullOrEmpty(token) || !_sessions.ContainsKey(token))
        {
            _logger.LogWarning("[Auth] Logout failed - invalid token");

            scope.Span.SetTag("logout.success", "false");
            scope.Span.SetTag("logout.failure_reason", "invalid_token");

            return false;
        }

        var (userId, _) = _sessions[token];
        _sessions.Remove(token);

        _logger.LogInformation("[Auth] User logged out: {UserId}", userId);

        scope.Span.SetTag("logout.success", "true");
        scope.Span.SetTag("user.id", userId);

        return true;
    }

    private static string GenerateToken(string userId)
    {
        // Simple token generation (in production, use JWT or similar)
        return $"{userId}-{Guid.NewGuid():N}";
    }
}
