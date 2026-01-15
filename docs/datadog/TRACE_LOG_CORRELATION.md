# Trace and Log Correlation - Configuration Guide

## Overview

Your .NET API now has **automatic trace and log correlation** enabled. This means every log message automatically includes the trace ID and span ID, allowing you to seamlessly navigate between logs and traces in the Datadog UI.

## What's Configured

### 1. JSON Console Logging

**File:** `Api/Program.cs`

```csharp
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
```

**Why JSON?**
- Structured logging for better parsing
- Datadog Agent can easily extract fields
- Supports nested objects and arrays
- Better performance than plain text parsing

### 2. Automatic Trace Injection

**File:** `Api/Dockerfile`

```dockerfile
ENV DD_LOGS_INJECTION=true
```

**What it does:**
- Datadog .NET tracer automatically injects trace context into logs
- No code changes required in your application
- Works with Microsoft.Extensions.Logging out of the box

### 3. Log Scopes with Trace IDs

When a request is being processed, Datadog automatically adds these fields to the log scope:

```json
{
  "dd_service": "datadog-maui-api",
  "dd_env": "local",
  "dd_version": "1.0.0",
  "dd_trace_id": "6955936500000000a93f542a18708f58",
  "dd_span_id": "18438398828528913963"
}
```

## Example Log Output

### Startup Logs (No Trace Context)
```json
{
  "Timestamp": "2025-12-31 21:19:14",
  "LogLevel": "Information",
  "Category": "DatadogMauiApi",
  "Message": "API Starting on port 8080...",
  "Scopes": [{
    "dd_service": "datadog-maui-api",
    "dd_env": "local",
    "dd_version": "1.0.0"
  }]
}
```

### Request Logs (With Trace Context)
```json
{
  "Timestamp": "2025-12-31 21:19:33",
  "LogLevel": "Information",
  "Category": "Program",
  "Message": "[Data Submission] CorrelationId: LOG-CORRELATION-TEST, SessionName: Log Test",
  "State": {
    "CorrelationId": "LOG-CORRELATION-TEST",
    "SessionName": "Log Test",
    "Notes": "Testing trace correlation",
    "NumericValue": 777
  },
  "Scopes": [{
    "dd_service": "datadog-maui-api",
    "dd_env": "local",
    "dd_version": "1.0.0",
    "dd_trace_id": "6955936500000000a93f542a18708f58",  ← Trace ID
    "dd_span_id": "18438398828528913963"  ← Span ID
  }]
}
```

## Testing Correlation

### 1. Local Testing

```bash
# Make an API call
curl -X POST http://localhost:5000/data \
  -H "Content-Type: application/json" \
  -d '{"CorrelationId":"test-123","SessionName":"Test","Notes":"Testing","NumericValue":42}'

# View logs with trace IDs
docker-compose logs api | grep "Data Submission" | tail -1 | jq '.Scopes[0]'

# Output shows:
# {
#   "dd_service": "datadog-maui-api",
#   "dd_env": "local",
#   "dd_version": "1.0.0",
#   "dd_trace_id": "...",
#   "dd_span_id": "..."
# }
```

### 2. In Datadog UI

#### From Logs to Traces:
1. Go to **Logs** in Datadog UI
2. Search for logs from service `datadog-maui-api`
3. Click on any log entry
4. You'll see a **"View Trace"** button that takes you directly to the associated trace

#### From Traces to Logs:
1. Go to **APM → Traces**
2. Click on any trace
3. Look for the **"Logs"** tab in the trace details
4. You'll see all logs that occurred during that trace

## Log Fields Explained

### Standard Fields
- `Timestamp`: When the log was written (UTC)
- `LogLevel`: Information, Warning, Error, etc.
- `Category`: Logger category (usually the class name)
- `Message`: The log message
- `State`: Structured data from the log parameters

### Datadog Trace Correlation Fields
- `dd_service`: Service name (for filtering and grouping)
- `dd_env`: Environment (dev, staging, prod, local)
- `dd_version`: Application version
- `dd_trace_id`: **Unique identifier for the entire request trace**
- `dd_span_id`: **Identifier for the current operation within the trace**

### Additional Request Context
- `SpanId`: ASP.NET Core activity span ID
- `TraceId`: ASP.NET Core activity trace ID
- `ConnectionId`: HTTP connection identifier
- `RequestId`: Unique request identifier
- `RequestPath`: The API endpoint being called

## Benefits

### 1. Faster Debugging
- See logs in context of the entire request
- Jump from error log directly to trace
- Understand what happened before/after an error

### 2. Complete Observability
- Correlate mobile app actions → API traces → logs
- End-to-end request flow visibility
- See timing and dependencies

### 3. Better Filtering
- Filter logs by trace ID to see only related logs
- Filter by span ID for specific operations
- Combine log and trace queries

## Logging Best Practices

### Use Structured Logging
```csharp
// ✅ Good - Structured parameters
logger.LogInformation(
    "[Data Submission] CorrelationId: {CorrelationId}, Value: {Value}",
    correlationId,
    numericValue
);

// ❌ Bad - String interpolation
logger.LogInformation($"[Data Submission] CorrelationId: {correlationId}, Value: {numericValue}");
```

**Why?** Structured parameters are extracted as fields in JSON logs, making them searchable in Datadog.

### Include Important Context
```csharp
// Add custom attributes to logs
using (logger.BeginScope(new Dictionary<string, object>
{
    ["user_id"] = userId,
    ["session_id"] = sessionId
}))
{
    logger.LogInformation("Processing user request");
    // All logs within this scope will have user_id and session_id
}
```

### Use Appropriate Log Levels
- `LogInformation`: Normal operations, business events
- `LogWarning`: Unexpected but handled situations
- `LogError`: Errors that need attention
- `LogDebug`: Detailed diagnostic information (filtered in production)

## Datadog Agent Configuration

The Datadog Agent automatically collects logs from containers:

```yaml
environment:
  - DD_LOGS_ENABLED=true
  - DD_LOGS_CONFIG_CONTAINER_COLLECT_ALL=true
```

Logs are:
1. Written to stdout/stderr by your API
2. Captured by Docker
3. Collected by Datadog Agent
4. Parsed and indexed in Datadog
5. Correlated with traces via `dd_trace_id`

## Advanced: Custom Log Correlation

If you want to add custom correlation (e.g., user ID, session ID):

```csharp
// In Program.cs, add to your endpoint handlers:
app.MapPost("/data", (DataSubmission submission, ILogger<Program> logger, HttpContext context) =>
{
    // Get trace context
    var traceId = Activity.Current?.TraceId.ToString();
    var spanId = Activity.Current?.SpanId.ToString();

    // Create a scope with custom fields
    using (logger.BeginScope(new Dictionary<string, object>
    {
        ["mobile_correlation_id"] = submission.CorrelationId,
        ["session_name"] = submission.SessionName,
        ["trace_id"] = traceId,
        ["span_id"] = spanId
    }))
    {
        logger.LogInformation(
            "[Data Submission] Processing submission",
            submission.CorrelationId
        );

        // Your processing logic...
    }

    return Results.Ok(new { /* ... */ });
});
```

## Viewing Correlated Data in Datadog

### 1. Unified Service View
**APM → Service → datadog-maui-api**
- See traces and logs together
- Monitor error rates across both
- Identify patterns in failures

### 2. Log Search with Trace Context
```
service:datadog-maui-api @dd.trace_id:"6955936500000000a93f542a18708f58"
```

### 3. Trace Search with Log Events
- In trace flame graph, see log events as markers
- Click log marker to see full log details
- See logs before/during/after the trace

## Troubleshooting

### Logs don't have dd_trace_id

**Check:**
1. `DD_LOGS_INJECTION=true` is set in Dockerfile
2. Datadog tracer is loaded (check startup logs)
3. Request actually triggered a trace
4. JSON console logging is configured

**Verify:**
```bash
docker exec datadog-maui-api env | grep DD_LOGS_INJECTION
# Should show: DD_LOGS_INJECTION=true
```

### Logs not appearing in Datadog UI

**Check:**
1. Datadog Agent is collecting logs: `DD_LOGS_ENABLED=true`
2. Container logs are being collected: `DD_LOGS_CONFIG_CONTAINER_COLLECT_ALL=true`
3. Agent status: `docker exec datadog-agent agent status | grep -A 10 "Logs Agent"`
4. API key is valid

### Trace IDs don't match between logs and traces

**Check:**
1. Verify both use the same format (128-bit trace IDs)
2. Check timezone differences (logs are UTC)
3. Ensure clock sync between containers

## Summary

✅ **You have:**
- JSON structured logging
- Automatic trace ID injection
- Log and trace correlation
- Complete observability stack

✅ **You can:**
- Jump from logs to traces
- Jump from traces to logs
- See complete request context
- Debug faster with correlated data

✅ **No additional code needed:**
- Automatic instrumentation handles everything
- Works with standard ASP.NET Core logging
- Zero performance overhead

---

**Status:** ✅ Fully Operational

Last verified: 2025-12-31
- Trace ID injection: Working
- Log correlation: Working
- JSON formatting: Working
- Datadog collection: Working
