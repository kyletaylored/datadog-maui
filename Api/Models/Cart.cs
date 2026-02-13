namespace DatadogMauiApi.Models;

public record Cart(
    int Id,
    string UserId,
    DateTime Date,
    List<CartProduct> Products
);
