using System.Collections.Concurrent;
using System.Diagnostics;
using DatadogMauiApi.Models;

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

// Health check endpoint
app.MapGet("/health", (ILogger<Program> logger) =>
{
    logger.LogInformation("[Health Check] Service is healthy at {Time}", DateTime.UtcNow);
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
})
.WithName("HealthCheck");

// Config endpoint - returns dynamic configuration
app.MapGet("/config", (ILogger<Program> logger, HttpContext context) =>
{
    // Extract correlation ID from headers if present
    if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
    {
        logger.LogInformation("[Config] Configuration requested with CorrelationId: {CorrelationId}", correlationId);
    }
    else
    {
        logger.LogInformation("[Config] Configuration requested");
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

    return Results.Ok(config);
})
.WithName("GetConfig");

// Data submission endpoint
app.MapPost("/data", (DataSubmission submission, ILogger<Program> logger) =>
{

    logger.LogInformation(
        "[Data Submission] CorrelationId: {CorrelationId}, SessionName: {SessionName}, Notes: {Notes}, NumericValue: {NumericValue}",
        submission.CorrelationId,
        submission.SessionName,
        submission.Notes,
        submission.NumericValue
    );

    // Store the submission
    dataStore.Add(submission);

    logger.LogInformation("[Data Store] Total submissions: {Count}", dataStore.Count);

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
app.MapGet("/data", (ILogger<Program> logger) =>
{
    logger.LogInformation("[Data Retrieval] Fetching all submissions. Count: {Count}", dataStore.Count);
    return Results.Ok(dataStore.ToList());
})
.WithName("GetAllData");

app.Logger.LogInformation("API Starting on port 8080...");
app.Logger.LogInformation("Web Portal: http://localhost:5000");
app.Logger.LogInformation("Access from Android Emulator: http://10.0.2.2:5000");
app.Logger.LogInformation("Access from iOS Simulator: http://localhost:5000");

app.Run();
