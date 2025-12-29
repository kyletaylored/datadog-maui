using DatadogMauiApp.Services;

namespace DatadogMauiApp.Pages;

public partial class DashboardPage : ContentPage
{
    private readonly ApiService _apiService;

    public DashboardPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
    }

    private async void OnSubmitClicked(object sender, EventArgs e)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(SessionNameEntry.Text))
        {
            await DisplayAlert("Validation Error", "Session Name is required", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(NotesEditor.Text))
        {
            await DisplayAlert("Validation Error", "Notes are required", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(NumericValueEntry.Text) ||
            !decimal.TryParse(NumericValueEntry.Text, out var numericValue))
        {
            await DisplayAlert("Validation Error", "Please enter a valid numeric value", "OK");
            return;
        }

        // Disable button during submission
        SubmitButton.IsEnabled = false;
        StatusLabel.Text = "Submitting...";
        StatusLabel.TextColor = Colors.Orange;

        try
        {
            // Generate correlation ID
            var correlationId = Guid.NewGuid().ToString();

            Console.WriteLine($"[Telemetry] Form Submitted - CorrelationID: {correlationId}");

            // Submit data
            var response = await _apiService.SubmitDataAsync(
                correlationId,
                SessionNameEntry.Text,
                NotesEditor.Text,
                numericValue
            );

            if (response.IsSuccessful)
            {
                StatusLabel.Text = $"Success! CorrelationID: {correlationId}";
                StatusLabel.TextColor = Colors.Green;

                // Clear form
                SessionNameEntry.Text = string.Empty;
                NotesEditor.Text = string.Empty;
                NumericValueEntry.Text = string.Empty;

                await DisplayAlert("Success", "Data submitted successfully!", "OK");
            }
            else
            {
                StatusLabel.Text = $"Error: {response.ErrorMessage}";
                StatusLabel.TextColor = Colors.Red;
                await DisplayAlert("Error", response.ErrorMessage ?? "Failed to submit data", "OK");
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            SubmitButton.IsEnabled = true;
        }
    }
}
