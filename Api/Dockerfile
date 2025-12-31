# check=skip=UndefinedVar
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY DatadogMauiApi.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

ARG TARGETARCH
ARG DD_GIT_REPOSITORY_URL
ARG DD_GIT_COMMIT_SHA
ARG DD_GIT_TAG
ARG BUILD_DATE
# Note: RUM client tokens are PUBLIC credentials meant for browser exposure
# They are not secret and are safe to include in client-side code
ARG DD_RUM_WEB_CLIENT_TOKEN
ARG DD_RUM_WEB_APPLICATION_ID

WORKDIR /app

# Install dependencies and Datadog .NET APM tracer
RUN apt-get update \
    && apt-get install -y unzip procps wget curl \
    && mkdir -p /opt/datadog \
    && mkdir -p /var/log/datadog \
    && TRACER_VERSION=3.34.0 \
    && curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm_${TRACER_VERSION}_${TARGETARCH}.deb \
    && dpkg -i ./datadog-dotnet-apm_${TRACER_VERSION}_${TARGETARCH}.deb \
    && /opt/datadog/createLogPath.sh \
    && rm ./datadog-dotnet-apm_${TRACER_VERSION}_${TARGETARCH}.deb \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Inject RUM credentials into index.html at build time
RUN if [ -f wwwroot/index.html ] && [ -n "$DD_RUM_WEB_CLIENT_TOKEN" ] && [ -n "$DD_RUM_WEB_APPLICATION_ID" ]; then \
    sed -i "s/PLACEHOLDER_CLIENT_TOKEN/${DD_RUM_WEB_CLIENT_TOKEN}/" wwwroot/index.html && \
    sed -i "s/PLACEHOLDER_APPLICATION_ID/${DD_RUM_WEB_APPLICATION_ID}/" wwwroot/index.html && \
    echo "✅ RUM credentials injected into index.html"; \
    else \
    echo "⚠️  Skipping RUM injection (file not found or credentials not provided)"; \
    fi

# Expose port 8080 (container internal port)
EXPOSE 8080

# Set environment to listen on all interfaces
ENV ASPNETCORE_URLS=http://+:8080

# Enable the Datadog tracer (automatic instrumentation)
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_DOTNET_TRACER_HOME=/opt/datadog
ENV DD_INTEGRATIONS=/opt/datadog/integrations.json

# Datadog configuration
ENV DD_SERVICE=datadog-maui-api
ENV DD_ENV=local
ENV DD_VERSION=1.0.0
ENV DD_TRACE_ENABLED=true
ENV DD_RUNTIME_METRICS_ENABLED=true
ENV DD_LOGS_INJECTION=true
ENV DD_TRACE_SAMPLE_RATE=1.0
ENV DD_PROFILING_ENABLED=1
ENV DD_TRACE_DEBUG=0

# Datadog Agent connection (will be overridden by docker-compose)
ENV DD_AGENT_HOST=datadog-agent
ENV DD_TRACE_AGENT_PORT=8126

# Datadog Git metadata (for CI/CD integration)
ENV DD_GIT_REPOSITORY_URL=${DD_GIT_REPOSITORY_URL}
ENV DD_GIT_COMMIT_SHA=${DD_GIT_COMMIT_SHA}
ENV DD_GIT_TAG=${DD_GIT_TAG}

# Add Docker labels for Datadog and OCI standards
LABEL org.opencontainers.image.created="${BUILD_DATE}" \
    org.opencontainers.image.source="${DD_GIT_REPOSITORY_URL}" \
    org.opencontainers.image.revision="${DD_GIT_COMMIT_SHA}" \
    org.opencontainers.image.version="${DD_GIT_TAG}" \
    org.opencontainers.image.title="Datadog MAUI API" \
    org.opencontainers.image.description=".NET API with Datadog APM tracing for MAUI mobile app" \
    com.datadoghq.tags.service="datadog-maui-api" \
    com.datadoghq.tags.env="local" \
    com.datadoghq.tags.version="${DD_GIT_TAG}" \
    git.repository_url="${DD_GIT_REPOSITORY_URL}" \
    git.commit.sha="${DD_GIT_COMMIT_SHA}" \
    git.tag="${DD_GIT_TAG}"

ENTRYPOINT ["dotnet", "DatadogMauiApi.dll"]
