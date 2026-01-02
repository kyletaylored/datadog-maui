using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DatadogMauiApp.Models;
using Microsoft.Extensions.Logging;
#if ANDROID
using Datadog.Android.Trace;
#endif

namespace DatadogMauiApp.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly ILogger<ApiService> _logger;

    public ApiService(ILogger<ApiService> logger, ILogger<DatadogHttpHandler> datadogLogger)
    {
        _logger = logger;

        // Create HttpClient with Datadog tracking handler
#if ANDROID
        var datadogHandler = new DatadogHttpHandler(datadogLogger);
        _httpClient = new HttpClient(datadogHandler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _logger.LogInformation("HttpClient created with Datadog tracking handler");
#else
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
#endif

        // Platform-specific base URL
        // Android emulator: 10.0.2.2 maps to host machine's localhost
        // iOS simulator: localhost works directly
        _baseUrl = GetPlatformBaseUrl();

        _logger.LogInformation("ApiService initialized with base URL: {BaseUrl}", _baseUrl);
    }

    private string GetPlatformBaseUrl()
    {
#if ANDROID
        return "http://10.0.2.2:5000";
#elif IOS
        return "http://localhost:5000";
#else
        return "http://localhost:5000";
#endif
    }

    public async Task<ApiResponse> SubmitDataAsync(
        string correlationId,
        string sessionName,
        string notes,
        decimal numericValue)
    {
        try
        {
            var submission = new DataSubmission(
                correlationId,
                sessionName,
                notes,
                numericValue
            );

            var json = JsonSerializer.Serialize(submission);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Submitting data to {Url}", $"{_baseUrl}/data");
            _logger.LogDebug("Payload: {Payload}", json);
            _logger.LogInformation("Correlation ID: {CorrelationId} (for distributed tracing)", correlationId);

            var response = await _httpClient.PostAsync($"{_baseUrl}/data", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response received: {ResponseContent}", responseContent);

                var apiResponse = JsonSerializer.Deserialize<ApiResponse>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                return apiResponse ?? new ApiResponse
                {
                    IsSuccessful = true,
                    CorrelationId = correlationId
                };
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error Response: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);

                return new ApiResponse
                {
                    IsSuccessful = false,
                    ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP Error: {ErrorMessage}", ex.Message);
            return new ApiResponse
            {
                IsSuccessful = false,
                ErrorMessage = $"Network error: {ex.Message}. Make sure the API is running at {_baseUrl}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error: {ErrorMessage}", ex.Message);
            return new ApiResponse
            {
                IsSuccessful = false,
                ErrorMessage = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<ConfigResponse?> GetConfigAsync(string? correlationId = null)
    {
        try
        {
            _logger.LogInformation("Fetching config from {Url}", $"{_baseUrl}/config");

            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/config");

            // Add correlation ID header for Datadog correlation
            if (!string.IsNullOrEmpty(correlationId))
            {
                request.Headers.Add("X-Correlation-ID", correlationId);
            }

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var config = await response.Content.ReadFromJsonAsync<ConfigResponse>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Config received: WebViewUrl={WebViewUrl}", config?.WebViewUrl);
                return config;
            }
            else
            {
                _logger.LogWarning("Config fetch failed: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching config");
            return null;
        }
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            _logger.LogInformation("Health check: {Url}", $"{_baseUrl}/health");

            var response = await _httpClient.GetAsync($"{_baseUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return false;
        }
    }

    public async Task<HealthResponse?> GetHealthAsync()
    {
        try
        {
            _logger.LogInformation("Fetching health from {Url}", $"{_baseUrl}/health");

            var response = await _httpClient.GetAsync($"{_baseUrl}/health");

            if (response.IsSuccessStatusCode)
            {
                var health = await response.Content.ReadFromJsonAsync<HealthResponse>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                _logger.LogInformation("Health received: Status={Status}", health?.Status);
                return health;
            }
            else
            {
                _logger.LogWarning("Health fetch failed: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching health");
            return null;
        }
    }
}
