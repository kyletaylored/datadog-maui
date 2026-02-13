using System.Collections.Concurrent;
using DatadogMauiApi.Models;

namespace DatadogMauiApi.Services;

public class CartStore
{
    private readonly ConcurrentDictionary<int, Cart> _carts;
    private readonly ILogger<CartStore> _logger;
    private int _nextId = 11; // Start after seed data

    public CartStore(ILogger<CartStore> logger)
    {
        _logger = logger;
        _carts = new ConcurrentDictionary<int, Cart>();
        InitializeCarts();
    }

    private void InitializeCarts()
    {
        var seedCarts = GetSeedCarts();
        foreach (var cart in seedCarts)
        {
            _carts.TryAdd(cart.Id, cart);
        }
        _logger.LogInformation("[CartStore] Initialized with {Count} carts for {Users} users",
            _carts.Count,
            _carts.Values.Select(c => c.UserId).Distinct().Count());
    }

    private static List<Cart> GetSeedCarts() => new()
    {
        new Cart(
            1,
            "user-001", // demo user
            DateTime.UtcNow.AddDays(-5),
            new List<CartProduct>
            {
                new CartProduct(1, 1), // Laptop
                new CartProduct(3, 1)  // Headphones
            }
        ),
        new Cart(
            2,
            "user-001",
            DateTime.UtcNow.AddDays(-3),
            new List<CartProduct>
            {
                new CartProduct(6, 2), // 2 T-Shirts
                new CartProduct(7, 1)  // Jeans
            }
        ),
        new Cart(
            3,
            "user-002", // admin user
            DateTime.UtcNow.AddDays(-7),
            new List<CartProduct>
            {
                new CartProduct(11, 1), // Coffee Maker
                new CartProduct(12, 1)  // Blender
            }
        ),
        new Cart(
            4,
            "user-002",
            DateTime.UtcNow.AddDays(-2),
            new List<CartProduct>
            {
                new CartProduct(16, 1), // Yoga Mat
                new CartProduct(17, 1)  // Dumbbell Set
            }
        ),
        new Cart(
            5,
            "user-003", // test user
            DateTime.UtcNow.AddDays(-10),
            new List<CartProduct>
            {
                new CartProduct(2, 1), // Smartphone
                new CartProduct(5, 1)  // Smart Watch
            }
        ),
        new Cart(
            6,
            "user-003",
            DateTime.UtcNow.AddDays(-1),
            new List<CartProduct>
            {
                new CartProduct(19, 1), // Bicycle
                new CartProduct(20, 2)  // 2 Soccer Balls
            }
        ),
        new Cart(
            7,
            "user-001",
            DateTime.UtcNow.AddDays(-15),
            new List<CartProduct>
            {
                new CartProduct(4, 1),  // Tablet
                new CartProduct(15, 2)  // 2 Throw Pillows
            }
        ),
        new Cart(
            8,
            "user-002",
            DateTime.UtcNow.AddDays(-20),
            new List<CartProduct>
            {
                new CartProduct(8, 1),  // Jacket
                new CartProduct(9, 1)   // Sneakers
            }
        ),
        new Cart(
            9,
            "user-003",
            DateTime.UtcNow.AddDays(-12),
            new List<CartProduct>
            {
                new CartProduct(13, 1), // Vacuum Cleaner
                new CartProduct(14, 1)  // Garden Tools
            }
        ),
        new Cart(
            10,
            "user-001",
            DateTime.UtcNow,
            new List<CartProduct>
            {
                new CartProduct(10, 1), // Hat
                new CartProduct(18, 1)  // Camping Tent
            }
        )
    };

    public IEnumerable<Cart> GetAll() => _carts.Values.OrderBy(c => c.Id);

    public Cart? GetById(int id) => _carts.TryGetValue(id, out var cart) ? cart : null;

    public IEnumerable<Cart> GetByUserId(string userId) =>
        _carts.Values
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Date);

    public IEnumerable<Cart> GetByDateRange(DateTime? startDate, DateTime? endDate)
    {
        var start = startDate ?? DateTime.MinValue;
        var end = endDate ?? DateTime.MaxValue;
        return _carts.Values
            .Where(c => c.Date >= start && c.Date <= end)
            .OrderBy(c => c.Id);
    }

    public Cart Add(Cart cart)
    {
        var newId = System.Threading.Interlocked.Increment(ref _nextId) - 1;
        var newCart = cart with { Id = newId };
        _carts.TryAdd(newCart.Id, newCart);
        _logger.LogInformation("[CartStore] Added cart: {CartId} for user: {UserId} with {ProductCount} products",
            newCart.Id, newCart.UserId, newCart.Products.Count);
        return newCart;
    }

    public Cart? Update(int id, Cart cart)
    {
        if (!_carts.ContainsKey(id))
        {
            _logger.LogWarning("[CartStore] Update failed - cart not found: {CartId}", id);
            return null;
        }

        var updated = cart with { Id = id };
        _carts[id] = updated;
        _logger.LogInformation("[CartStore] Updated cart: {CartId} for user: {UserId}", id, updated.UserId);
        return updated;
    }

    public Cart? Delete(int id)
    {
        _carts.TryRemove(id, out var deleted);
        if (deleted != null)
        {
            _logger.LogInformation("[CartStore] Deleted cart: {CartId} for user: {UserId}", id, deleted.UserId);
        }
        else
        {
            _logger.LogWarning("[CartStore] Delete failed - cart not found: {CartId}", id);
        }
        return deleted;
    }
}
