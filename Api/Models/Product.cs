namespace DatadogMauiApi.Models;

public record Product(
    int Id,
    string Title,
    decimal Price,
    string Description,
    string Image,
    string Category
);
