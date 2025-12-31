# Datadog Integration - Backend API Setup Guide

## Overview

This guide explains how the Datadog APM (Application Performance Monitoring) has been integrated into the ASP.NET Core Web API backend. The integration provides distributed tracing, performance monitoring, and correlation with mobile RUM sessions.

---

## What's Already Configured

### ✅ NuGet Packages Added

[Api/DatadogMauiApi.csproj](Api/DatadogMauiApi.csproj) now includes:
- `Datadog.Trace` (v3.11.0) - Core tracing library
- `Datadog.Trace.Bundle` (v3.11.0) - Bundled tracer with auto-instrumentation

### ✅ Datadog Configuration in Code

[Api/Program.cs](Api/Program.cs) includes:
- Environment variable configuration for Datadog
- Custom tracing spans for all endpoints
- Correlation ID tagging for RUM-to-APM correlation
- Trace and Span ID returned in responses

### ✅ Dockerfile Configuration

[Api/Dockerfile](Api/Dockerfile) includes:
- Datadog environment variables
- Configuration for service name, environment, and version
- Runtime metrics and log injection enabled

### ✅ API Endpoint Tracing

All API endpoints have custom tracing:

**Health Endpoint** (`/health`):
```csharp
using var scope = Tracer.Instance.StartActive("health.check");
scope.Span.SetTag("check.type", "health");
```

**Config Endpoint** (`/config`):
```csharp
using var scope = Tracer.Instance.StartActive("config.get");
// Extracts X-Correlation-ID header and tags the trace
scope.Span.SetTag("correlation.id", correlationId.ToString());
```

**Data Submission Endpoint** (`/data` POST):
```csharp
using var scope = Tracer.Instance.StartActive("data.submit");
scope.Span.SetTag("correlation.id", submission.CorrelationId);
scope.Span.SetTag("session.name", submission.SessionName);
scope.Span.SetTag("numeric.value", submission.NumericValue.ToString());
// Returns traceId and spanId for correlation
```

**Data Retrieval Endpoint** (`/data` GET):
```csharp
using var scope = Tracer.Instance.StartActive("data.getall");
scope.Span.SetTag("data.count", dataStore.Count.ToString());
```

### ✅ Mobile Integration

[MauiApp/Services/ApiService.cs](MauiApp/Services/ApiService.cs) has been updated:
- `GetConfigAsync()` now sends `X-Correlation-ID` header
- This allows the API to correlate traces with mobile RUM sessions

---

## How It Works

### 1. Automatic Instrumentation

The Datadog tracer automatically instruments:
- **ASP.NET Core**: HTTP requests and responses
- **HttpClient**: Outbound HTTP calls
- **ADO.NET**: Database queries (if added)
- **Logging**: Automatic trace and span ID injection

### 2. Custom Instrumentation

Each endpoint creates a custom span with:
- **Operation Name**: Identifies the operation (e.g., `data.submit`, `config.get`)
- **Tags**: Metadata for filtering and analysis
- **Correlation ID**: Links mobile RUM sessions to API traces

### 3. RUM-to-APM Correlation

The correlation flow:
1. Mobile app generates `correlationId` (GUID)
2. Mobile app sends `correlationId` in request body or `X-Correlation-ID` header
3. API tags the trace with `correlation.id`
4. Datadog correlates mobile RUM session with backend API trace
5. You can navigate from mobile session → API trace in Datadog dashboard

---

## Configuration Options

### Environment Variables

Set these via Docker, docker-compose, or environment:

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `DD_SERVICE` | Service name | `datadog-maui-api` | Yes |
| `DD_ENV` | Environment | `dev` | Yes |
| `DD_VERSION` | App version | `1.0.0` | No |
| `DD_TRACE_ENABLED` | Enable tracing | `true` | Yes |
| `DD_RUNTIME_METRICS_ENABLED` | Enable runtime metrics | `true` | No |
| `DD_LOGS_INJECTION` | Inject trace IDs in logs | `true` | No |
| `DD_AGENT_HOST` | Datadog Agent host | `localhost` | No |
| `DD_AGENT_PORT` | Datadog Agent port | `8126` | No |
| `DD_SITE` | Datadog site | `datadoghq.com` | No |

### Via Docker Compose

Add to `docker-compose.yml`:
```yaml
services:
  api:
    environment:
      - DD_SERVICE=datadog-maui-api
      - DD_ENV=production
      - DD_VERSION=1.0.0
      - DD_AGENT_HOST=datadog-agent
      - DD_SITE=datadoghq.com
```

### Via Docker Run

```bash
docker run -d \
  -e DD_SERVICE=datadog-maui-api \
  -e DD_ENV=dev \
  -e DD_VERSION=1.0.0 \
  -e DD_AGENT_HOST=host.docker.internal \
  -p 5000:8080 \
  datadog-maui-api
```

---

## Datadog Agent Setup

To send traces to Datadog, you need the Datadog Agent running.

### Option 1: Datadog Agent as Container (Recommended for Local Dev)

Create a `docker-compose.yml` or run:

```bash
docker run -d \
  --name datadog-agent \
  -e DD_API_KEY=<YOUR_DATADOG_API_KEY> \
  -e DD_SITE=datadoghq.com \
  -e DD_APM_ENABLED=true \
  -e DD_APM_NON_LOCAL_TRAFFIC=true \
  -p 8126:8126 \
  -p 8125:8125/udp \
  datadog/agent:latest
```

Then set in your API container:
```bash
-e DD_AGENT_HOST=host.docker.internal  # macOS/Windows
# or
-e DD_AGENT_HOST=172.17.0.1            # Linux
```

### Option 2: Datadog Agent on Host Machine

1. Install Datadog Agent: https://docs.datadoghq.com/agent/
2. Configure APM in `/etc/datadog-agent/datadog.yaml`:
```yaml
apm_config:
  enabled: true
  apm_non_local_traffic: true
```
3. Restart agent: `sudo systemctl restart datadog-agent`
4. API will use `localhost:8126` by default

### Option 3: Datadog Serverless/Cloud (Production)

For production deployments (AWS, Azure, GCP):
- Use Datadog Lambda Extension (AWS Lambda)
- Use Datadog Agent sidecar (Kubernetes)
- Use Azure App Service integration
- See: https://docs.datadoghq.com/tracing/setup_overview/

---

## Testing the Integration

### 1. Start the API

```bash
make api-build
make api-start
```

### 2. Send Test Request

```bash
curl -X POST http://localhost:5000/data \
  -H "Content-Type: application/json" \
  -d '{
    "correlationId": "test-123",
    "sessionName": "Test Session",
    "notes": "Testing Datadog",
    "numericValue": 42.5
  }'
```

### 3. Check Response

The response includes trace and span IDs:
```json
{
  "message": "Data received successfully",
  "correlationId": "test-123",
  "timestamp": "2025-12-29T...",
  "traceId": "1234567890123456789",
  "spanId": "9876543210987654321"
}
```

### 4. View in Datadog

1. Go to [APM Traces](https://app.datadoghq.com/apm/traces)
2. Filter by `service:datadog-maui-api`
3. Find trace with your correlation ID
4. Click to see:
   - Trace timeline
   - Span details
   - Tags (correlation.id, session.name, etc.)
   - Logs (if enabled)

### 5. Test RUM-to-APM Correlation

1. Submit data from mobile app
2. Go to [RUM Explorer](https://app.datadoghq.com/rum/explorer)
3. Find your mobile session
4. Click on the API resource call
5. Click "View Trace" to jump to backend trace

---

## Features Enabled

### ✅ Distributed Tracing
- End-to-end request tracing
- Mobile → API correlation
- Multi-service tracing (if you add more services)

### ✅ Performance Monitoring
- Request duration
- Throughput (requests/second)
- Error rates
- P50, P75, P95, P99 latencies

### ✅ Runtime Metrics
- CPU usage
- Memory usage
- Thread count
- Garbage collection metrics

### ✅ Log Correlation
- Trace ID and Span ID injected into logs
- View logs alongside traces
- Filter logs by trace

### ✅ Custom Tags
- `correlation.id` - Link to mobile RUM session
- `session.name` - User session name
- `numeric.value` - Business metric
- `data.count` - Data store size

---

## Viewing Traces in Datadog

### APM Service Overview

1. Go to [APM → Services](https://app.datadoghq.com/apm/services)
2. Find `datadog-maui-api`
3. View:
   - Request rate
   - Error rate
   - Latency percentiles
   - Top endpoints

### APM Traces

1. Go to [APM → Traces](https://app.datadoghq.com/apm/traces)
2. Filter: `service:datadog-maui-api`
3. Search by correlation ID: `@correlation.id:test-123`
4. Click trace to see:
   - Flame graph
   - Span timeline
   - All tags
   - Related logs

### RUM-to-APM Correlation

1. Go to [RUM → Sessions](https://app.datadoghq.com/rum/sessions)
2. Find mobile session
3. Click session → Resources tab
4. Find API call
5. Click "View Trace" button
6. See full backend trace with mobile context

---

## Troubleshooting

### No Traces Appearing

**Check 1: Is the Datadog Agent running?**
```bash
# If using container
docker ps | grep datadog-agent

# If using host agent
sudo systemctl status datadog-agent
```

**Check 2: Can API reach the agent?**
```bash
# From API container
docker exec -it datadog-maui-api curl http://host.docker.internal:8126
```

**Check 3: Are traces enabled?**
```bash
docker logs datadog-maui-api | grep -i datadog
```

**Check 4: API Key correct?**
```bash
docker logs datadog-agent | grep -i api
```

### Traces Not Correlated with Mobile RUM

**Check 1: Correlation ID sent from mobile?**
- View mobile logs for `CorrelationId: {id}`
- Check API logs for `CorrelationId: {id}`

**Check 2: Correlation ID tagged in trace?**
- View trace in Datadog
- Check tags for `correlation.id`

**Check 3: RUM and APM in same Datadog account?**
- Verify both use same Datadog site (us1, eu1, etc.)

### High Latency or Performance Issues

**Check 1: Sample rate too high?**
- Reduce sample rate in production
- Set `DD_TRACE_SAMPLE_RATE=0.1` (10%)

**Check 2: Too many spans?**
- Reduce custom spans
- Use span filtering

**Check 3: Agent overloaded?**
- Increase agent resources
- Use multiple agents

---

## Sample Rate Configuration

For production, adjust sample rates to reduce costs:

```bash
# Trace 100% of errors, 10% of successful requests
-e DD_TRACE_SAMPLE_RATE=0.1
-e DD_TRACE_SAMPLING_RULES='[{"sample_rate":1.0,"service":"datadog-maui-api","resource":"*error*"}]'
```

---

## Production Checklist

Before deploying to production:

- [ ] Set `DD_ENV=production`
- [ ] Set appropriate `DD_VERSION`
- [ ] Configure Datadog Agent in production environment
- [ ] Set sample rate to reasonable value (10-50%)
- [ ] Enable log forwarding to Datadog
- [ ] Set up alerts for error rates and latency
- [ ] Test RUM-to-APM correlation
- [ ] Document custom tags for your team
- [ ] Set up dashboards for key metrics

---

## Advanced: Custom Metrics

Add custom metrics in your code:

```csharp
using Datadog.Trace;

// Increment counter
Tracer.Instance.TracerManager.Metrics.Increment("custom.metric.name");

// Record distribution
Tracer.Instance.TracerManager.Metrics.Distribution("request.size", submission.Size);

// Set gauge
Tracer.Instance.TracerManager.Metrics.Gauge("data.store.size", dataStore.Count);
```

---

## Resources

- [Datadog APM Documentation](https://docs.datadoghq.com/tracing/)
- [Datadog .NET Tracer](https://docs.datadoghq.com/tracing/setup_overview/setup/dotnet-core/)
- [Datadog Agent Setup](https://docs.datadoghq.com/agent/)
- [RUM-to-APM Correlation](https://docs.datadoghq.com/real_user_monitoring/connect_rum_and_traces/)
- [Datadog API](https://docs.datadoghq.com/api/)

---

## Summary

✅ **NuGet Packages**: Datadog.Trace and Datadog.Trace.Bundle added
✅ **Automatic Instrumentation**: ASP.NET Core requests traced automatically
✅ **Custom Spans**: All endpoints have custom spans with tags
✅ **Correlation**: Correlation ID links mobile RUM to backend traces
✅ **Runtime Metrics**: CPU, memory, GC metrics enabled
✅ **Log Injection**: Trace and span IDs in logs
✅ **Docker Ready**: Environment variables configured in Dockerfile

**Status**: Ready to test! Start the Datadog Agent and make API calls to see traces.

**Next Steps**:
1. Start Datadog Agent (see "Datadog Agent Setup" section)
2. Rebuild and restart API: `make api-build && make api-restart`
3. Send test requests: `make api-test`
4. View traces in [Datadog APM](https://app.datadoghq.com/apm/traces)
5. Test from mobile app to verify RUM-to-APM correlation
