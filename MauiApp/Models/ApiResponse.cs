namespace DatadogMauiApp.Models;

public class ApiResponse
{
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
}
