using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DatadogMauiApp.Models;

namespace DatadogMauiApp.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Platform-specific base URL
        // Android emulator: 10.0.2.2 maps to host machine's localhost
        // iOS simulator: localhost works directly
        _baseUrl = GetPlatformBaseUrl();

        Console.WriteLine($"[ApiService] Base URL: {_baseUrl}");
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

            Console.WriteLine($"[ApiService] Submitting data to {_baseUrl}/data");
            Console.WriteLine($"[ApiService] Payload: {json}");

            var response = await _httpClient.PostAsync($"{_baseUrl}/data", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[ApiService] Response: {responseContent}");

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
                Console.WriteLine($"[ApiService] Error Response: {errorContent}");

                return new ApiResponse
                {
                    IsSuccessful = false,
                    ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[ApiService] HTTP Error: {ex.Message}");
            return new ApiResponse
            {
                IsSuccessful = false,
                ErrorMessage = $"Network error: {ex.Message}. Make sure the API is running at {_baseUrl}"
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] Error: {ex.Message}");
            return new ApiResponse
            {
                IsSuccessful = false,
                ErrorMessage = $"Error: {ex.Message}"
            };
        }
    }

    public async Task<ConfigResponse?> GetConfigAsync()
    {
        try
        {
            Console.WriteLine($"[ApiService] Fetching config from {_baseUrl}/config");

            var response = await _httpClient.GetAsync($"{_baseUrl}/config");

            if (response.IsSuccessStatusCode)
            {
                var config = await response.Content.ReadFromJsonAsync<ConfigResponse>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                Console.WriteLine($"[ApiService] Config received: WebViewUrl={config?.WebViewUrl}");
                return config;
            }
            else
            {
                Console.WriteLine($"[ApiService] Config fetch failed: {response.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] Error fetching config: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> HealthCheckAsync()
    {
        try
        {
            Console.WriteLine($"[ApiService] Health check: {_baseUrl}/health");

            var response = await _httpClient.GetAsync($"{_baseUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ApiService] Health check failed: {ex.Message}");
            return false;
        }
    }
}
