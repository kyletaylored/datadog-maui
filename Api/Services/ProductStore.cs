using System.Collections.Concurrent;
using DatadogMauiApi.Models;

namespace DatadogMauiApi.Services;

public class ProductStore
{
    private readonly ConcurrentDictionary<int, Product> _products;
    private readonly ILogger<ProductStore> _logger;
    private int _nextId = 21; // Start after seed data

    public ProductStore(ILogger<ProductStore> logger)
    {
        _logger = logger;
        _products = new ConcurrentDictionary<int, Product>();
        InitializeProducts();
    }

    private void InitializeProducts()
    {
        var seedProducts = GetSeedProducts();
        foreach (var product in seedProducts)
        {
            _products.TryAdd(product.Id, product);
        }
        _logger.LogInformation("[ProductStore] Initialized with {Count} products across {Categories} categories",
            _products.Count,
            _products.Values.Select(p => p.Category).Distinct().Count());
    }

    private static List<Product> GetSeedProducts() => new()
    {
        // Electronics (5 items)
        new Product(1, "Laptop", 799.99m, "High-performance laptop with 16GB RAM", "https://example.com/laptop.jpg", "electronics"),
        new Product(2, "Smartphone", 699.99m, "Latest model smartphone with 128GB storage", "https://example.com/phone.jpg", "electronics"),
        new Product(3, "Wireless Headphones", 149.99m, "Noise-cancelling wireless headphones", "https://example.com/headphones.jpg", "electronics"),
        new Product(4, "Tablet", 449.99m, "10-inch tablet with stylus support", "https://example.com/tablet.jpg", "electronics"),
        new Product(5, "Smart Watch", 299.99m, "Fitness tracking smart watch", "https://example.com/watch.jpg", "electronics"),

        // Clothing (5 items)
        new Product(6, "T-Shirt", 19.99m, "Cotton t-shirt in various colors", "https://example.com/tshirt.jpg", "clothing"),
        new Product(7, "Jeans", 49.99m, "Classic fit denim jeans", "https://example.com/jeans.jpg", "clothing"),
        new Product(8, "Jacket", 89.99m, "All-weather jacket with hood", "https://example.com/jacket.jpg", "clothing"),
        new Product(9, "Sneakers", 79.99m, "Comfortable running sneakers", "https://example.com/sneakers.jpg", "clothing"),
        new Product(10, "Hat", 24.99m, "Adjustable baseball cap", "https://example.com/hat.jpg", "clothing"),

        // Home & Garden (5 items)
        new Product(11, "Coffee Maker", 89.99m, "Programmable coffee maker with timer", "https://example.com/coffee.jpg", "home"),
        new Product(12, "Blender", 59.99m, "High-speed blender for smoothies", "https://example.com/blender.jpg", "home"),
        new Product(13, "Vacuum Cleaner", 199.99m, "Cordless vacuum with HEPA filter", "https://example.com/vacuum.jpg", "home"),
        new Product(14, "Garden Tools Set", 49.99m, "Complete set of gardening tools", "https://example.com/tools.jpg", "home"),
        new Product(15, "Throw Pillow", 29.99m, "Decorative throw pillow", "https://example.com/pillow.jpg", "home"),

        // Sports & Outdoors (5 items)
        new Product(16, "Yoga Mat", 34.99m, "Non-slip yoga mat with carrying strap", "https://example.com/yoga.jpg", "sports"),
        new Product(17, "Dumbbell Set", 99.99m, "Adjustable dumbbell set 5-50 lbs", "https://example.com/dumbbells.jpg", "sports"),
        new Product(18, "Camping Tent", 149.99m, "4-person waterproof camping tent", "https://example.com/tent.jpg", "sports"),
        new Product(19, "Bicycle", 399.99m, "Mountain bike with 21 speeds", "https://example.com/bike.jpg", "sports"),
        new Product(20, "Soccer Ball", 24.99m, "Official size soccer ball", "https://example.com/soccer.jpg", "sports")
    };

    public IEnumerable<Product> GetAll() => _products.Values.OrderBy(p => p.Id);

    public Product? GetById(int id) => _products.TryGetValue(id, out var product) ? product : null;

    public IEnumerable<string> GetCategories() =>
        _products.Values
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c);

    public IEnumerable<Product> GetByCategory(string category) =>
        _products.Values
            .Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Id);

    public Product Add(Product product)
    {
        var newId = System.Threading.Interlocked.Increment(ref _nextId) - 1;
        var newProduct = product with { Id = newId };
        _products.TryAdd(newProduct.Id, newProduct);
        _logger.LogInformation("[ProductStore] Added product: {ProductId} - {Title}", newProduct.Id, newProduct.Title);
        return newProduct;
    }

    public Product? Update(int id, Product product)
    {
        if (!_products.ContainsKey(id))
        {
            _logger.LogWarning("[ProductStore] Update failed - product not found: {ProductId}", id);
            return null;
        }

        var updated = product with { Id = id };
        _products[id] = updated;
        _logger.LogInformation("[ProductStore] Updated product: {ProductId} - {Title}", id, updated.Title);
        return updated;
    }

    public Product? Delete(int id)
    {
        _products.TryRemove(id, out var deleted);
        if (deleted != null)
        {
            _logger.LogInformation("[ProductStore] Deleted product: {ProductId} - {Title}", id, deleted.Title);
        }
        else
        {
            _logger.LogWarning("[ProductStore] Delete failed - product not found: {ProductId}", id);
        }
        return deleted;
    }
}
