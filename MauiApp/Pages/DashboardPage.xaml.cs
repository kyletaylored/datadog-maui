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
            await DisplayAlertAsync("Validation Error", "Session Name is required", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(NotesEditor.Text))
        {
            await DisplayAlertAsync("Validation Error", "Notes are required", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(NumericValueEntry.Text) ||
            !decimal.TryParse(NumericValueEntry.Text, out var numericValue))
        {
            await DisplayAlertAsync("Validation Error", "Please enter a valid numeric value", "OK");
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

                await DisplayAlertAsync("Success", "Data submitted successfully!", "OK");
            }
            else
            {
                StatusLabel.Text = $"Error: {response.ErrorMessage}";
                StatusLabel.TextColor = Colors.Red;
                await DisplayAlertAsync("Error", response.ErrorMessage ?? "Failed to submit data", "OK");
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
            await DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            SubmitButton.IsEnabled = true;
        }
    }

    private async void OnTestCrashClicked(object sender, EventArgs e)
    {
        // Confirm with user before crashing
        var confirmed = await DisplayAlertAsync(
            "⚠️ Test Crash",
            "This will intentionally crash the app to test crash reporting and dSYM symbolication.\n\n" +
            "After the crash:\n" +
            "1. Build a Release version\n" +
            "2. Upload dSYMs to Datadog\n" +
            "3. Check crash reports in Datadog\n\n" +
            "Continue?",
            "Yes, Crash App",
            "Cancel"
        );

        if (!confirmed)
            return;

        Console.WriteLine("[TestCrash] User initiated test crash for dSYM symbolication testing");
        Console.WriteLine("[TestCrash] Location: DashboardPage.xaml.cs:OnTestCrashClicked");

        // Log to Datadog before crashing (if available)
        try
        {
#if IOS
            // iOS-specific logging before crash
            // Note: DDLogger API needs investigation - using Console for now
            Console.WriteLine("[Datadog] Test crash initiated by user for dSYM testing");
#elif ANDROID
            // Android-specific crash logging
            var logger = Datadog.Android.Log.Logger.Builder.Instance?.Build();
            logger?.E("Test crash initiated by user for NDK testing");
#endif
        }
        catch
        {
            // Ignore logging errors - we're about to crash anyway
        }

        // Wait a moment to ensure log is sent
        await Task.Delay(500);

        // Intentional crash with meaningful stack trace
        throw new InvalidOperationException(
            "TEST CRASH: This is an intentional crash to test Datadog crash reporting and dSYM symbolication. " +
            "This crash was triggered from DashboardPage.xaml.cs:OnTestCrashClicked(). " +
            "If you see this symbolicated in Datadog, dSYM upload worked correctly!"
        );
    }
}
