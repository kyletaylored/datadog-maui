# ✅ Trace and Log Correlation - COMPLETE

## Summary

Successfully implemented **automatic trace and log correlation** for your .NET API. Every log message now includes trace IDs, enabling seamless navigation between logs and traces in Datadog.

## What's Working

### 1. JSON Structured Logging ✅
Logs are now output in JSON format with all fields properly structured:

```json
{
  "Timestamp": "2025-12-31 21:19:33",
  "LogLevel": "Information",
  "Category": "Program",
  "Message": "[Data Submission] CorrelationId: LOG-CORRELATION-TEST",
  "State": {
    "CorrelationId": "LOG-CORRELATION-TEST",
    "SessionName": "Log Test",
    "Notes": "Testing trace correlation",
    "NumericValue": 777
  },
  "Scopes": [...]
}
```

### 2. Automatic Trace ID Injection ✅
Every log during a request includes Datadog trace context:

```json
"Scopes": [{
  "dd_service": "datadog-maui-api",
  "dd_env": "local",
  "dd_version": "1.0.0",
  "dd_trace_id": "6955936500000000a93f542a18708f58",  ← Links to APM trace
  "dd_span_id": "18438398828528913963"  ← Links to specific span
}]
```

### 3. Request Context ✅
Additional correlation fields automatically included:

```json
{
  "SpanId": "1ebb6e2705a83a14",
  "TraceId": "5c6814b2f9aef96f54f85a01cd15eb90",
  "ParentId": "0000000000000000",
  "ConnectionId": "0HNI8M95F62N4",
  "RequestId": "0HNI8M95F62N4:00000001",
  "RequestPath": "/data"
}
```

## Configuration Changes

### Api/Program.cs
```csharp
// Added JSON console logging with scope support
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

### Api/Dockerfile (Already Set)
```dockerfile
ENV DD_LOGS_INJECTION=true  ← Enables automatic trace ID injection
```

### docker-compose.yml (Already Set)
```yaml
environment:
  - DD_LOGS_ENABLED=true
  - DD_LOGS_CONFIG_CONTAINER_COLLECT_ALL=true
```

## Testing

### Local Test
```bash
# Make API call
curl -X POST http://localhost:5000/data \
  -H "Content-Type: application/json" \
  -d '{"CorrelationId":"test-123","SessionName":"Test","Notes":"Testing","NumericValue":42}'

# View logs with trace IDs
docker-compose logs api | grep "Data Submission" | tail -1

# You'll see dd_trace_id and dd_span_id in the output
```

### Example Output
```json
{
  "Timestamp": "2025-12-31 21:19:33",
  "LogLevel": "Information",
  "Message": "[Data Submission] CorrelationId: LOG-CORRELATION-TEST, SessionName: Log Test, Notes: Testing trace correlation, NumericValue: 777",
  "Scopes": [{
    "dd_service": "datadog-maui-api",
    "dd_env": "local",
    "dd_version": "1.0.0",
    "dd_trace_id": "6955936500000000a93f542a18708f58",
    "dd_span_id": "18438398828528913963"
  }]
}
```

## In Datadog UI

### From Logs → Traces
1. Go to **Logs** in Datadog
2. Filter by `service:datadog-maui-api`
3. Click any log entry
4. Look for **"View Trace"** button
5. Click to jump to the associated APM trace

### From Traces → Logs
1. Go to **APM → Traces**
2. Filter by `service:datadog-maui-api env:local`
3. Click any trace
4. Look for **"Logs"** tab
5. See all logs that occurred during that trace

### Search by Trace ID
```
service:datadog-maui-api @dd.trace_id:"6955936500000000a93f542a18708f58"
```

## Complete Observability Flow

```
Mobile App (Android)
  ↓
  Generates Correlation ID
  ↓
HTTP Request to API
  ↓
.NET API receives request
  ↓
  ┌─────────────────────────────┐
  │ Datadog Tracer creates:     │
  │ - Trace ID                  │
  │ - Span ID                   │
  └─────────────────────────────┘
  ↓
API processes request
  ↓
  ┌─────────────────────────────┐
  │ Logs written with:          │
  │ - dd_trace_id (injected)    │
  │ - dd_span_id (injected)     │
  │ - correlation_id (custom)   │
  │ - request context           │
  └─────────────────────────────┘
  ↓
  ┌───────────┬─────────────┐
  ↓           ↓             ↓
Traces    Logs         Metrics
  ↓           ↓             ↓
Datadog Agent (port 8126, logs collection)
  ↓
Datadog Cloud
  ↓
  ┌─────────────────────────────┐
  │ Correlated View:            │
  │ - Mobile RUM Session        │
  │   ↓                         │
  │ - Backend Trace             │
  │   ↓                         │
  │ - API Logs                  │
  │   ↓                         │
  │ - Infrastructure Metrics    │
  └─────────────────────────────┘
```

## Key Benefits

### 1. Unified Debugging
- See logs in context of the entire request trace
- Jump from error log directly to the trace showing what happened
- Understand the sequence of events

### 2. Mobile → Backend Correlation
- Mobile correlation ID links to backend trace
- Backend trace ID links to all logs
- Complete visibility from user action to server logs

### 3. Performance Analysis
- See which operations logged errors
- Correlate slow operations with log events
- Identify bottlenecks with timing + logs

### 4. Error Investigation
- When an error occurs, see:
  - The trace showing the request flow
  - All logs before and during the error
  - Infrastructure metrics at that time
  - Related mobile session

## Logging Best Practices

### ✅ Use Structured Logging
```csharp
// Good - Structured parameters become searchable fields
logger.LogInformation(
    "[Data Submission] CorrelationId: {CorrelationId}, Value: {Value}",
    correlationId,
    numericValue
);
```

### ✅ Include Important Context
```csharp
// All logs within scope will have these fields
using (logger.BeginScope(new Dictionary<string, object>
{
    ["user_id"] = userId,
    ["mobile_session_id"] = sessionId
}))
{
    logger.LogInformation("Processing user request");
}
```

### ✅ Use Appropriate Log Levels
- **LogInformation**: Normal operations
- **LogWarning**: Unexpected but handled
- **LogError**: Errors needing attention
- **LogDebug**: Detailed diagnostics

## Technical Details

### Trace ID Format
- 128-bit trace ID (hex string)
- Consistent across logs and traces
- Example: `6955936500000000a93f542a18708f58`

### Span ID Format
- 64-bit span ID (decimal number)
- Identifies specific operation within trace
- Example: `18438398828528913963`

### Log Collection
- **Source**: Docker stdout/stderr
- **Format**: JSON (structured)
- **Agent**: Datadog Agent collects automatically
- **Destination**: Datadog Logs (agent-http-intake.logs.datadoghq.com)

### Automatic Injection
The Datadog .NET tracer intercepts the logging pipeline and:
1. Detects active trace context
2. Adds `dd_trace_id` and `dd_span_id` to log scope
3. Injects service, environment, and version tags
4. No code changes required

## Documentation

- **[TRACE_LOG_CORRELATION.md](TRACE_LOG_CORRELATION.md)** - Detailed guide
- **[DATADOG_AGENT_SETUP.md](DATADOG_AGENT_SETUP.md)** - Agent configuration
- **[SETUP_COMPLETE.md](SETUP_COMPLETE.md)** - Overall setup status

## Verification Checklist

- ✅ JSON console logging configured
- ✅ `DD_LOGS_INJECTION=true` set
- ✅ Logs include `dd_trace_id`
- ✅ Logs include `dd_span_id`
- ✅ Logs include `dd_service`, `dd_env`, `dd_version`
- ✅ Structured parameters work (`State` object)
- ✅ Request context included (SpanId, TraceId, etc.)
- ✅ Datadog Agent collecting logs
- ✅ Logs sent to Datadog cloud

## Next Steps

### 1. Add Custom Correlation Fields
```csharp
// Add mobile correlation ID to all logs in a request
using (logger.BeginScope(new Dictionary<string, object>
{
    ["mobile_correlation_id"] = submission.CorrelationId,
    ["user_id"] = submission.UserId
}))
{
    // All logs will have these fields
}
```

### 2. Create Log-Based Monitors
In Datadog UI:
- Create alerts based on log patterns
- Example: Alert when logs contain "Error" + specific trace ID

### 3. Build Dashboards
- Show error rates from logs
- Display traces with high error counts
- Correlate with infrastructure metrics

### 4. Enable iOS Logging
- Configure Datadog SDK for iOS
- Same correlation will work automatically

---

**Status:** ✅ **FULLY OPERATIONAL**

**Verified:** 2025-12-31
- Trace ID injection: ✅ Working
- Span ID injection: ✅ Working
- JSON formatting: ✅ Working
- Structured logging: ✅ Working
- Datadog collection: ✅ Configured

**Logging Framework:** Microsoft.Extensions.Logging (ASP.NET Core built-in)
**Trace Injection:** Automatic via Datadog .NET Tracer v3.34.0
**No additional libraries required!**
