using System.Collections.Concurrent;
using System.Diagnostics;
using Datadog.Trace;
using DatadogMauiApi.Models;
using DatadogMauiApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging with JSON formatting for Datadog
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = false
    };
});

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSingleton<SessionManager>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// In-memory data store
var dataStore = new ConcurrentBag<DataSubmission>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

// Serve static files (web portal)
// IMPORTANT: UseDefaultFiles must be called before UseStaticFiles
app.UseDefaultFiles();
app.UseStaticFiles();

// Authentication endpoints
app.MapPost("/auth/login", (LoginRequest request, SessionManager sessionManager, ILogger<Program> logger) =>
{
    logger.LogInformation("[Auth] Login request for username: {Username}", request.Username);

    var response = sessionManager.AuthenticateUser(request.Username, request.Password);

    if (response.Success)
    {
        logger.LogInformation("[Auth] Login successful for user: {Username}, UserId: {UserId}",
            response.Username, response.UserId);
    }
    else
    {
        logger.LogWarning("[Auth] Login failed for username: {Username}", request.Username);
    }

    return response.Success ? Results.Ok(response) : Results.Unauthorized();
})
.WithName("Login");

app.MapPost("/auth/logout", (SessionManager sessionManager, ILogger<Program> logger, HttpContext context) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

    if (string.IsNullOrEmpty(token))
    {
        logger.LogWarning("[Auth] Logout attempted without token");
        return Results.BadRequest(new { message = "No token provided" });
    }

    var success = sessionManager.Logout(token);

    if (success)
    {
        logger.LogInformation("[Auth] Logout successful");
        return Results.Ok(new { message = "Logged out successfully" });
    }

    logger.LogWarning("[Auth] Logout failed");
    return Results.BadRequest(new { message = "Logout failed" });
})
.WithName("Logout");

app.MapGet("/profile", (SessionManager sessionManager, ILogger<Program> logger, HttpContext context) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

    if (string.IsNullOrEmpty(token))
    {
        logger.LogWarning("[Profile] Access attempted without token");
        return Results.Unauthorized();
    }

    var (isValid, userId) = sessionManager.ValidateSession(token);

    if (!isValid || userId == null)
    {
        logger.LogWarning("[Profile] Invalid session token");
        return Results.Unauthorized();
    }

    var profile = sessionManager.GetUserProfile(userId);

    if (profile == null)
    {
        logger.LogWarning("[Profile] Profile not found for userId: {UserId}", userId);
        return Results.NotFound(new { message = "Profile not found" });
    }

    logger.LogInformation("[Profile] Profile retrieved for userId: {UserId}", userId);
    return Results.Ok(profile);
})
.WithName("GetProfile");

app.MapPut("/profile", (UserProfile updatedProfile, SessionManager sessionManager, ILogger<Program> logger, HttpContext context) =>
{
    var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

    if (string.IsNullOrEmpty(token))
    {
        logger.LogWarning("[Profile] Update attempted without token");
        return Results.Unauthorized();
    }

    var (isValid, userId) = sessionManager.ValidateSession(token);

    if (!isValid || userId == null)
    {
        logger.LogWarning("[Profile] Invalid session token for profile update");
        return Results.Unauthorized();
    }

    // Ensure user can only update their own profile
    if (userId != updatedProfile.UserId)
    {
        logger.LogWarning("[Profile] User {UserId} attempted to update profile for {TargetUserId}",
            userId, updatedProfile.UserId);
        return Results.Forbid();
    }

    var success = sessionManager.UpdateUserProfile(userId, updatedProfile.FullName, updatedProfile.Email);

    if (success)
    {
        logger.LogInformation("[Profile] Profile updated for userId: {UserId}", userId);
        return Results.Ok(new { message = "Profile updated successfully" });
    }

    logger.LogWarning("[Profile] Profile update failed for userId: {UserId}", userId);
    return Results.BadRequest(new { message = "Profile update failed" });
})
.WithName("UpdateProfile");

// Health check endpoint
app.MapGet("/health", (SessionManager sessionManager, ILogger<Program> logger, HttpContext context) =>
{
    // Get the active span created by automatic instrumentation
    var activeScope = Tracer.Instance.ActiveScope;
    if (activeScope != null)
    {
        activeScope.Span.ResourceName = "GET /health";
        activeScope.Span.SetTag("custom.operation.type", "health_check");

        // Check if user is authenticated
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        if (!string.IsNullOrEmpty(token))
        {
            var (isValid, userId) = sessionManager.ValidateSession(token);
            if (isValid && userId != null)
            {
                activeScope.Span.SetTag("custom.user.id", userId);
                activeScope.Span.SetTag("custom.authenticated", "true");
                logger.LogInformation("[Health Check] Service is healthy at {Time} - User: {UserId}", DateTime.UtcNow, userId);
            }
            else
            {
                activeScope.Span.SetTag("custom.authenticated", "false");
                logger.LogInformation("[Health Check] Service is healthy at {Time} - Anonymous", DateTime.UtcNow);
            }
        }
        else
        {
            activeScope.Span.SetTag("custom.authenticated", "false");
            logger.LogInformation("[Health Check] Service is healthy at {Time} - Anonymous", DateTime.UtcNow);
        }
    }

    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck");

// Config endpoint - returns dynamic configuration
app.MapGet("/config", (SessionManager sessionManager, ILogger<Program> logger, HttpContext context) =>
{
    // Get the active span created by automatic instrumentation
    var activeScope = Tracer.Instance.ActiveScope;
    if (activeScope != null)
    {
        activeScope.Span.ResourceName = "GET /config";
        activeScope.Span.SetTag("custom.operation.type", "config_fetch");

        // Extract correlation ID from headers if present
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            activeScope.Span.SetTag("custom.correlation.id", correlationId.ToString());
            logger.LogInformation("[Config] Configuration requested with CorrelationId: {CorrelationId}", correlationId);
        }
        else
        {
            logger.LogInformation("[Config] Configuration requested");
        }

        // Check if user is authenticated
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        if (!string.IsNullOrEmpty(token))
        {
            var (isValid, userId) = sessionManager.ValidateSession(token);
            if (isValid && userId != null)
            {
                activeScope.Span.SetTag("custom.user.id", userId);
                activeScope.Span.SetTag("custom.authenticated", "true");
                logger.LogInformation("[Config] Configuration requested by user: {UserId}", userId);
            }
            else
            {
                activeScope.Span.SetTag("custom.authenticated", "false");
            }
        }
        else
        {
            activeScope.Span.SetTag("custom.authenticated", "false");
        }

        // Return the web portal URL served from this container
        // Android emulator uses 10.0.2.2 to access host machine
        // iOS simulator can use localhost
        var config = new ConfigResponse(
            WebViewUrl: "http://10.0.2.2:5000",
            FeatureFlags: new Dictionary<string, bool>
            {
                { "EnableTelemetry", true },
                { "EnableAdvancedFeatures", false }
            }
        );

        activeScope.Span.SetTag("custom.config.webview_url", config.WebViewUrl);
        activeScope.Span.SetTag("custom.config.feature_flags_count", config.FeatureFlags.Count.ToString());

        return Results.Ok(config);
    }

    // Fallback if no active span
    return Results.Ok(new ConfigResponse(
        WebViewUrl: "http://10.0.2.2:5000",
        FeatureFlags: new Dictionary<string, bool>
        {
            { "EnableTelemetry", true },
            { "EnableAdvancedFeatures", false }
        }
    ));
})
.WithName("GetConfig");

// Data submission endpoint
app.MapPost("/data", (DataSubmission submission, SessionManager sessionManager, ILogger<Program> logger, HttpContext context) =>
{
    // Get the active span created by automatic instrumentation
    var activeScope = Tracer.Instance.ActiveScope;
    if (activeScope != null)
    {
        activeScope.Span.ResourceName = "POST /data";
        activeScope.Span.SetTag("custom.operation.type", "data_submission");
        activeScope.Span.SetTag("custom.correlation.id", submission.CorrelationId);
        activeScope.Span.SetTag("custom.data.session_name", submission.SessionName);
        activeScope.Span.SetTag("custom.data.numeric_value", submission.NumericValue.ToString());

        // Check if user is authenticated
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        if (!string.IsNullOrEmpty(token))
        {
            var (isValid, userId) = sessionManager.ValidateSession(token);
            if (isValid && userId != null)
            {
                activeScope.Span.SetTag("custom.user.id", userId);
                activeScope.Span.SetTag("custom.authenticated", "true");
                logger.LogInformation(
                    "[Data Submission] User: {UserId}, CorrelationId: {CorrelationId}, SessionName: {SessionName}, Notes: {Notes}, NumericValue: {NumericValue}",
                    userId,
                    submission.CorrelationId,
                    submission.SessionName,
                    submission.Notes,
                    submission.NumericValue
                );
            }
            else
            {
                activeScope.Span.SetTag("custom.authenticated", "false");
                logger.LogInformation(
                    "[Data Submission] CorrelationId: {CorrelationId}, SessionName: {SessionName}, Notes: {Notes}, NumericValue: {NumericValue}",
                    submission.CorrelationId,
                    submission.SessionName,
                    submission.Notes,
                    submission.NumericValue
                );
            }
        }
        else
        {
            activeScope.Span.SetTag("custom.authenticated", "false");
            logger.LogInformation(
                "[Data Submission] CorrelationId: {CorrelationId}, SessionName: {SessionName}, Notes: {Notes}, NumericValue: {NumericValue}",
                submission.CorrelationId,
                submission.SessionName,
                submission.Notes,
                submission.NumericValue
            );
        }

        // Store the submission
        dataStore.Add(submission);

        logger.LogInformation("[Data Store] Total submissions: {Count}", dataStore.Count);

        activeScope.Span.SetTag("custom.data.total_submissions", dataStore.Count.ToString());
        activeScope.Span.SetTag("custom.submission.success", "true");
    }

    return Results.Ok(new
    {
        isSuccessful = true,
        message = "Data received successfully",
        correlationId = submission.CorrelationId,
        timestamp = DateTime.UtcNow
    });
})
.WithName("SubmitData");

// Bonus: Get all submitted data (for debugging)
app.MapGet("/data", (SessionManager sessionManager, ILogger<Program> logger, HttpContext context) =>
{
    // Get the active span created by automatic instrumentation
    var activeScope = Tracer.Instance.ActiveScope;
    if (activeScope != null)
    {
        activeScope.Span.ResourceName = "GET /data";
        activeScope.Span.SetTag("custom.operation.type", "data_retrieval");
        activeScope.Span.SetTag("custom.data.count", dataStore.Count.ToString());

        // Check if user is authenticated
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
        if (!string.IsNullOrEmpty(token))
        {
            var (isValid, userId) = sessionManager.ValidateSession(token);
            if (isValid && userId != null)
            {
                activeScope.Span.SetTag("custom.user.id", userId);
                activeScope.Span.SetTag("custom.authenticated", "true");
                logger.LogInformation("[Data Retrieval] Fetching all submissions for user: {UserId}. Count: {Count}", userId, dataStore.Count);
            }
            else
            {
                activeScope.Span.SetTag("custom.authenticated", "false");
                logger.LogInformation("[Data Retrieval] Fetching all submissions. Count: {Count}", dataStore.Count);
            }
        }
        else
        {
            activeScope.Span.SetTag("custom.authenticated", "false");
            logger.LogInformation("[Data Retrieval] Fetching all submissions. Count: {Count}", dataStore.Count);
        }
    }

    return Results.Ok(dataStore.ToList());
})
.WithName("GetAllData");

app.Logger.LogInformation("API Starting on port 8080...");
app.Logger.LogInformation("Web Portal: http://localhost:5000");
app.Logger.LogInformation("Access from Android Emulator: http://10.0.2.2:5000");
app.Logger.LogInformation("Access from iOS Simulator: http://localhost:5000");

app.Run();
