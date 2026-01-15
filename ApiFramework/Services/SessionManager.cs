using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Datadog.Trace;
using DatadogMauiApi.Framework.Models;

namespace DatadogMauiApi.Framework.Services
{
    public class SessionManager
    {
        // Singleton instance to share sessions across all controllers
        private static readonly Lazy<SessionManager> _instance = new Lazy<SessionManager>(() => new SessionManager());
        public static SessionManager Instance => _instance.Value;

        private readonly ConcurrentDictionary<string, UserProfile> _users;
        private readonly ConcurrentDictionary<string, Tuple<string, DateTime>> _sessions;

        private SessionManager()
        {
            _users = new ConcurrentDictionary<string, UserProfile>();
            _sessions = new ConcurrentDictionary<string, Tuple<string, DateTime>>();

            // Initialize demo users
            _users.TryAdd("demo", new UserProfile
            {
                UserId = "user-001",
                Username = "demo",
                Email = "demo@example.com",
                FullName = "Demo User",
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            });

            _users.TryAdd("admin", new UserProfile
            {
                UserId = "user-002",
                Username = "admin",
                Email = "admin@example.com",
                FullName = "Admin User",
                CreatedAt = DateTime.UtcNow.AddDays(-60)
            });

            _users.TryAdd("test", new UserProfile
            {
                UserId = "user-003",
                Username = "test",
                Email = "test@example.com",
                FullName = "Test User",
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            });
        }

        public LoginResponse AuthenticateUser(string username, string password)
        {
            using (var scope = Tracer.Instance.StartActive("auth.login"))
            {
                scope.Span.ResourceName = "SessionManager.AuthenticateUser";
                scope.Span.SetTag("service.name", "datadog-maui-api-framework");
                scope.Span.SetTag("auth.username", username);
                scope.Span.SetTag("auth.method", "password");
                scope.Span.SetTag("service.operation", "user_login");

                // Simple password check (in production, use proper password hashing)
                if (!_users.ContainsKey(username) || password != "password")
                {
                    scope.Span.SetTag("auth.success", "false");
                    scope.Span.SetTag("auth.failure_reason", "invalid_credentials");

                    return new LoginResponse
                    {
                        Success = false,
                        Token = null,
                        Username = null,
                        UserId = null,
                        Message = "Invalid username or password"
                    };
                }

                var user = _users[username];
                var token = GenerateToken(user.UserId);

                // Store session
                _sessions.TryAdd(token, Tuple.Create(user.UserId, DateTime.UtcNow.AddHours(24)));

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                _users[username] = user;

                scope.Span.SetTag("auth.success", "true");
                scope.Span.SetTag("user.id", user.UserId);
                scope.Span.SetTag("user.username", username);
                scope.Span.SetTag("user.email", user.Email);

                return new LoginResponse
                {
                    Success = true,
                    Token = token,
                    Username = user.Username,
                    UserId = user.UserId,
                    Message = "Login successful"
                };
            }
        }

        public Tuple<bool, string> ValidateSession(string token)
        {
            using (var scope = Tracer.Instance.StartActive("auth.validate_session"))
            {
                scope.Span.ResourceName = "SessionManager.ValidateSession";
                scope.Span.SetTag("service.name", "datadog-maui-api-framework");
                scope.Span.SetTag("auth.token_length", token?.Length ?? 0);

                if (string.IsNullOrEmpty(token) || !_sessions.ContainsKey(token))
                {
                    scope.Span.SetTag("session.valid", "false");
                    scope.Span.SetTag("session.failure_reason", "token_not_found");
                    return Tuple.Create(false, (string)null);
                }

                var session = _sessions[token];
                var userId = session.Item1;
                var expiresAt = session.Item2;

                if (DateTime.UtcNow > expiresAt)
                {
                    _sessions.TryRemove(token, out _);
                    scope.Span.SetTag("session.valid", "false");
                    scope.Span.SetTag("session.failure_reason", "token_expired");
                    scope.Span.SetTag("user.id", userId);
                    return Tuple.Create(false, (string)null);
                }

                scope.Span.SetTag("session.valid", "true");
                scope.Span.SetTag("user.id", userId);
                return Tuple.Create(true, userId);
            }
        }

        public UserProfile GetUserProfile(string userId)
        {
            using (var scope = Tracer.Instance.StartActive("user.get_profile"))
            {
                scope.Span.ResourceName = "SessionManager.GetUserProfile";
                scope.Span.SetTag("service.name", "datadog-maui-api-framework");
                scope.Span.SetTag("user.id", userId);
                scope.Span.SetTag("operation.type", "profile_fetch");

                var user = _users.Values.FirstOrDefault(u => u.UserId == userId);

                if (user == null)
                {
                    scope.Span.SetTag("profile.found", "false");
                    return null;
                }

                scope.Span.SetTag("profile.found", "true");
                scope.Span.SetTag("user.username", user.Username);
                scope.Span.SetTag("user.email", user.Email);

                return user;
            }
        }

        public bool UpdateUserProfile(string userId, string fullName, string email)
        {
            using (var scope = Tracer.Instance.StartActive("user.update_profile"))
            {
                scope.Span.ResourceName = "SessionManager.UpdateUserProfile";
                scope.Span.SetTag("service.name", "datadog-maui-api-framework");
                scope.Span.SetTag("user.id", userId);
                scope.Span.SetTag("operation.type", "profile_update");
                scope.Span.SetTag("update.fields", "fullName,email");

                var user = _users.Values.FirstOrDefault(u => u.UserId == userId);

                if (user == null)
                {
                    scope.Span.SetTag("update.success", "false");
                    scope.Span.SetTag("update.failure_reason", "user_not_found");
                    return false;
                }

                // Update the user profile
                user.FullName = fullName;
                user.Email = email;
                _users[user.Username] = user;

                scope.Span.SetTag("update.success", "true");
                scope.Span.SetTag("user.username", user.Username);
                scope.Span.SetTag("user.email", email);

                return true;
            }
        }

        public bool Logout(string token)
        {
            using (var scope = Tracer.Instance.StartActive("auth.logout"))
            {
                scope.Span.ResourceName = "SessionManager.Logout";
                scope.Span.SetTag("service.name", "datadog-maui-api-framework");

                if (string.IsNullOrEmpty(token) || !_sessions.ContainsKey(token))
                {
                    scope.Span.SetTag("logout.success", "false");
                    scope.Span.SetTag("logout.failure_reason", "invalid_token");
                    return false;
                }

                var session = _sessions[token];
                var userId = session.Item1;
                _sessions.TryRemove(token, out _);

                scope.Span.SetTag("logout.success", "true");
                scope.Span.SetTag("user.id", userId);

                return true;
            }
        }

        private static string GenerateToken(string userId)
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + "-" + userId;
        }
    }
}
