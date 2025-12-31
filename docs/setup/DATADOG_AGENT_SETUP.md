# Datadog Agent Docker Setup

## Overview

The Docker Compose configuration now includes a Datadog Agent container that collects APM traces, logs, and metrics from the .NET API backend.

## Architecture

```
Mobile App (Android/iOS)
    |
    | HTTP Requests with Correlation IDs
    ↓
.NET API Container (datadog-maui-api)
    |
    | Traces, Logs, Metrics
    ↓
Datadog Agent Container (datadog-agent)
    |
    | Forwards to Datadog Cloud
    ↓
Datadog Platform (datadoghq.com)
```

## Configuration Files

### .env File
Located at project root, contains:
```
DD_API_KEY=REDACTED_API_KEY_1
DD_SITE=datadoghq.com
DD_ENV=local
```

### docker-compose.yml
Two services configured:
1. **datadog-agent**: Datadog Agent container
2. **api**: .NET API with Datadog .NET APM tracer

## Datadog Agent Configuration

**Image:** `datadog/agent:latest`

**Key Environment Variables:**
- `DD_API_KEY`: Your Datadog API key (from .env)
- `DD_SITE`: Datadog site (datadoghq.com)
- `DD_ENV`: Environment tag (local)
- `DD_APM_ENABLED=true`: Enable APM trace collection
- `DD_APM_NON_LOCAL_TRAFFIC=true`: Accept traces from other containers
- `DD_LOGS_ENABLED=true`: Enable log collection
- `DD_LOGS_CONFIG_CONTAINER_COLLECT_ALL=true`: Collect logs from all containers

**Ports:**
- `8126`: APM trace intake port
- `8125/udp`: DogStatsD metrics port

**Volumes:**
- Docker socket: Monitor container metrics
- `/proc/`: Host process information
- `/sys/fs/cgroup/`: Container resource metrics

## .NET API Configuration

**Datadog .NET Tracer Installation:**
The Dockerfile installs the Datadog .NET APM tracer version 3.34.0 using the official `.deb` package for automatic instrumentation.

**Required Environment Variables:**
```dockerfile
# CLR Profiler Configuration (enables automatic instrumentation)
CORECLR_ENABLE_PROFILING=1
CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
DD_DOTNET_TRACER_HOME=/opt/datadog
LD_PRELOAD=/opt/datadog/continuousprofiler/Datadog.Linux.ApiWrapper.x64.so

# Datadog Configuration
DD_SERVICE=datadog-maui-api
DD_ENV=local
DD_VERSION=1.0.0
DD_TRACE_ENABLED=true
DD_RUNTIME_METRICS_ENABLED=true
DD_LOGS_INJECTION=true
DD_TRACE_SAMPLE_RATE=1.0

# Agent Connection
DD_AGENT_HOST=datadog-agent
DD_TRACE_AGENT_PORT=8126
```

**NuGet Packages Installed:**
- `Datadog.Trace` (v3.7.0)
- `Datadog.Trace.Bundle` (v3.7.0)

**Note:** The NuGet packages provide manual instrumentation capabilities, but automatic instrumentation is enabled via the CLR profiler for complete tracing coverage.

## Usage

### Start the Stack
```bash
# Start both Datadog Agent and API
docker-compose up -d

# View logs
docker-compose logs -f

# View Datadog Agent logs specifically
docker-compose logs -f datadog-agent

# View API logs
docker-compose logs -f api
```

### Stop the Stack
```bash
docker-compose down
```

### Rebuild After Changes
```bash
# Rebuild API container
docker-compose up -d --build api

# Rebuild everything
docker-compose up -d --build
```

## Testing Distributed Tracing

### Quick Test from Command Line:
```bash
# Make a test API call
curl -X POST http://localhost:5000/data \
  -H "Content-Type: application/json" \
  -d '{"CorrelationId":"test-123","SessionName":"Test","Notes":"Testing","NumericValue":42}'

# Response will include real trace and span IDs:
# {"isSuccessful":true,"traceId":"5381752283447135596","spanId":"6978587585324817958",...}

# Wait 30 seconds and check agent status
docker exec datadog-agent agent status | grep -A 20 "APM Agent"

# You should see traces received from .NET 9.0.11 client 3.34.0.0
```

### From Mobile App:
1. Open the **API Test** tab in the mobile app
2. Click **Submit Test Data**
3. The app generates a correlation ID and sends it in the request
4. The correlation ID links the mobile RUM session to the backend trace

### Expected Flow:
1. **Mobile App** → Generates correlation ID (e.g., `REDACTED_APP_ID_1`)
2. **HTTP Request** → Includes correlation ID in request body
3. **.NET API** → Processes request, automatically traced by Datadog .NET tracer (v3.34.0)
4. **Datadog Agent** → Receives trace spans from API
5. **Datadog Cloud** → Correlates mobile RUM session with backend trace

### Verify Locally (Agent Status):
```bash
# Check that traces are being received
docker exec datadog-agent agent status 2>&1 | grep -A 30 "APM Agent"

# Expected output:
#   From .NET 9.0.11 (.NET), client 3.34.0.0
#     Traces received: 4 (7,174 bytes)
#     Spans received: 6
#   Priority sampling rate for 'service:datadog-maui-api,env:local': 100.0%
```

### Verify in Datadog UI:
1. Go to **APM → Traces** in Datadog UI (app.datadoghq.com)
2. Look for service `datadog-maui-api` with environment `local`
3. Click on a trace to see spans (e.g., `data.submit`, `health.check`, `config.get`)
4. Look for correlation ID in trace attributes (`correlation.id` tag)
5. Go to **RUM → Sessions** to see mobile sessions
6. Click on a session with API calls to see linked backend traces
7. Traces should appear within 1-2 minutes of making the API call

## Datadog Features Enabled

### In Datadog Agent:
- ✅ APM trace collection
- ✅ Log collection from containers
- ✅ DogStatsD metrics
- ✅ Container monitoring

### In .NET API:
- ✅ Automatic instrumentation (HTTP requests, database queries, etc.)
- ✅ Runtime metrics (.NET CLR metrics)
- ✅ Log injection (adds trace IDs to logs)
- ✅ 100% trace sampling

### In Mobile App (Android):
- ✅ RUM (Real User Monitoring)
- ✅ Session Replay
- ✅ APM correlation via correlation IDs
- ✅ Logs
- ✅ Crash reporting

## Troubleshooting

### Agent Not Starting
```bash
# Check agent logs
docker-compose logs datadog-agent

# Common issues:
# - Invalid DD_API_KEY in .env
# - Missing .env file
# - Port 8126 already in use
```

### API Not Sending Traces
```bash
# Check API logs for Datadog tracer messages
docker-compose logs api | grep -i datadog

# Verify tracer is loaded
docker exec -it datadog-maui-api ls -la /opt/datadog

# Check environment variables
docker exec -it datadog-maui-api env | grep DD_
```

### No Traces in Datadog UI
1. Wait 1-2 minutes for traces to appear
2. Verify DD_API_KEY is correct in .env
3. Check agent status: `docker exec -it datadog-agent agent status`
4. Verify agent can reach Datadog: `docker exec -it datadog-agent agent check connectivity`

### Correlation IDs Not Linking
1. Ensure correlation IDs are being generated in mobile app
2. Check API logs for correlation ID values
3. Verify correlation IDs are passed in HTTP request body/headers
4. Look for correlation ID in Datadog trace attributes

## Next Steps

1. **Test the Setup**: Make API calls from mobile app and verify traces appear in Datadog
2. **Custom Instrumentation**: Add custom spans to track specific operations
3. **Alerts**: Set up monitors for error rates, latency, etc.
4. **Dashboards**: Create dashboards showing mobile→backend flow
5. **iOS Support**: Configure Datadog SDK for iOS (currently Android only)

## Resources

- [Datadog .NET APM Documentation](https://docs.datadoghq.com/tracing/setup_overview/setup/dotnet-core/)
- [Datadog Agent Docker Documentation](https://docs.datadoghq.com/agent/docker/)
- [Distributed Tracing Guide](https://docs.datadoghq.com/tracing/trace_collection/custom_instrumentation/)
