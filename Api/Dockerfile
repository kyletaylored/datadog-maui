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
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 8080 (container internal port)
EXPOSE 8080

# Set environment to listen on all interfaces
ENV ASPNETCORE_URLS=http://+:8080

# Datadog configuration
# These can be overridden via docker-compose.yml or docker run -e
ENV DD_SERVICE=datadog-maui-api
ENV DD_ENV=dev
ENV DD_VERSION=1.0.0
ENV DD_TRACE_ENABLED=true
ENV DD_RUNTIME_METRICS_ENABLED=true
ENV DD_LOGS_INJECTION=true

# Optional: Set Datadog Agent host (defaults to localhost)
# Override with -e DD_AGENT_HOST=<host> if using Datadog Agent container
# ENV DD_AGENT_HOST=datadog-agent

# Optional: Set Datadog site (defaults to datadoghq.com)
# ENV DD_SITE=datadoghq.com

ENTRYPOINT ["dotnet", "DatadogMauiApi.dll"]
