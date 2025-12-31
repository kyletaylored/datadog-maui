using DatadogMauiApp.Services;
using System.Text.Json;

namespace DatadogMauiApp.Pages;

public partial class ApiTestPage : ContentPage
{
    private readonly ApiService _apiService;

    public ApiTestPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnHealthCheckClicked(object sender, EventArgs e)
    {
        HealthCheckButton.IsEnabled = false;
        HealthStatusLabel.Text = "Status: Checking...";
        HealthStatusLabel.TextColor = Colors.Orange;
        HealthDetailsLabel.IsVisible = false;

        try
        {
            var health = await _apiService.GetHealthAsync();

            if (health != null)
            {
                HealthStatusLabel.Text = $"Status: {health.Status}";
                HealthStatusLabel.TextColor = health.Status.Equals("Healthy", StringComparison.OrdinalIgnoreCase)
                    ? Colors.Green
                    : Colors.Red;

                var details = $"Environment: {health.Environment}\n" +
                             $"Timestamp: {health.Timestamp:yyyy-MM-dd HH:mm:ss}\n" +
                             $"Uptime: {health.Uptime}";

                HealthDetailsLabel.Text = details;
                HealthDetailsLabel.IsVisible = true;

                // Show in response details
                var json = JsonSerializer.Serialize(health, new JsonSerializerOptions { WriteIndented = true });
                ResponseDetailsLabel.Text = $"Health Check Response:\n\n{json}";
                ResponseDetailsLabel.TextColor = Colors.Black;

                Console.WriteLine($"[Telemetry] Health Check: {health.Status}");
            }
            else
            {
                HealthStatusLabel.Text = "Status: Failed to get health";
                HealthStatusLabel.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            HealthStatusLabel.Text = $"Status: Error - {ex.Message}";
            HealthStatusLabel.TextColor = Colors.Red;
            ResponseDetailsLabel.Text = $"Error:\n{ex.Message}";
            ResponseDetailsLabel.TextColor = Colors.Red;
        }
        finally
        {
            HealthCheckButton.IsEnabled = true;
        }
    }

    private async void OnGetConfigClicked(object sender, EventArgs e)
    {
        GetConfigButton.IsEnabled = false;
        ConfigStatusLabel.Text = "Status: Loading...";
        ConfigStatusLabel.TextColor = Colors.Orange;
        ConfigDetailsLabel.IsVisible = false;

        try
        {
            var config = await _apiService.GetConfigAsync();

            if (config != null)
            {
                ConfigStatusLabel.Text = "Status: Loaded";
                ConfigStatusLabel.TextColor = Colors.Green;

                var featureFlags = string.Join("\n", config.FeatureFlags.Select(kv => $"  • {kv.Key}: {kv.Value}"));
                var details = $"WebView URL: {config.WebViewUrl}\n\nFeature Flags:\n{featureFlags}";

                ConfigDetailsLabel.Text = details;
                ConfigDetailsLabel.IsVisible = true;

                // Show in response details
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                ResponseDetailsLabel.Text = $"Configuration Response:\n\n{json}";
                ResponseDetailsLabel.TextColor = Colors.Black;

                Console.WriteLine($"[Telemetry] Config Loaded - WebView URL: {config.WebViewUrl}");
            }
            else
            {
                ConfigStatusLabel.Text = "Status: Failed to get config";
                ConfigStatusLabel.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            ConfigStatusLabel.Text = $"Status: Error - {ex.Message}";
            ConfigStatusLabel.TextColor = Colors.Red;
            ResponseDetailsLabel.Text = $"Error:\n{ex.Message}";
            ResponseDetailsLabel.TextColor = Colors.Red;
        }
        finally
        {
            GetConfigButton.IsEnabled = true;
        }
    }

    private async void OnSubmitTestDataClicked(object sender, EventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(ApiTestSessionEntry.Text))
        {
            await DisplayAlertAsync("Validation Error", "Session Name is required", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(ApiTestNotesEditor.Text))
        {
            await DisplayAlertAsync("Validation Error", "Notes are required", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(ApiTestValueEntry.Text) ||
            !decimal.TryParse(ApiTestValueEntry.Text, out var numericValue))
        {
            await DisplayAlertAsync("Validation Error", "Please enter a valid numeric value", "OK");
            return;
        }

        SubmitTestDataButton.IsEnabled = false;
        SubmitStatusLabel.Text = "Status: Submitting...";
        SubmitStatusLabel.TextColor = Colors.Orange;
        SubmitDetailsLabel.IsVisible = false;

        try
        {
            var correlationId = Guid.NewGuid().ToString();

            Console.WriteLine($"[Telemetry] API Test Submit - CorrelationID: {correlationId}");

            var response = await _apiService.SubmitDataAsync(
                correlationId,
                ApiTestSessionEntry.Text,
                ApiTestNotesEditor.Text,
                numericValue
            );

            if (response.IsSuccessful)
            {
                SubmitStatusLabel.Text = "Status: Success!";
                SubmitStatusLabel.TextColor = Colors.Green;

                var details = $"Correlation ID: {correlationId}\n" +
                             $"Message: {response.Message}";

                SubmitDetailsLabel.Text = details;
                SubmitDetailsLabel.IsVisible = true;

                // Show in response details
                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
                ResponseDetailsLabel.Text = $"Submit Data Response:\n\n{json}";
                ResponseDetailsLabel.TextColor = Colors.Black;

                await DisplayAlertAsync("Success", $"Data submitted successfully!\nCorrelation ID: {correlationId}", "OK");
            }
            else
            {
                SubmitStatusLabel.Text = $"Status: Failed";
                SubmitStatusLabel.TextColor = Colors.Red;

                SubmitDetailsLabel.Text = response.ErrorMessage ?? "Unknown error";
                SubmitDetailsLabel.IsVisible = true;

                await DisplayAlertAsync("Error", response.ErrorMessage ?? "Failed to submit data", "OK");
            }
        }
        catch (Exception ex)
        {
            SubmitStatusLabel.Text = "Status: Error";
            SubmitStatusLabel.TextColor = Colors.Red;

            SubmitDetailsLabel.Text = ex.Message;
            SubmitDetailsLabel.IsVisible = true;

            ResponseDetailsLabel.Text = $"Error:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            ResponseDetailsLabel.TextColor = Colors.Red;

            await DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            SubmitTestDataButton.IsEnabled = true;
        }
    }
}
