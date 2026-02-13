using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DatadogMauiApi.Framework.Models;

namespace DatadogMauiApi.Framework.Services
{
    public class CartStore
    {
        private static readonly Lazy<CartStore> _instance = new Lazy<CartStore>(() => new CartStore());
        public static CartStore Instance => _instance.Value;

        private readonly ConcurrentDictionary<int, Cart> _carts;
        private int _nextId = 11; // Start after seed data

        private CartStore()
        {
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
        }

        private static List<Cart> GetSeedCarts() => new List<Cart>
        {
            new Cart
            {
                Id = 1,
                UserId = "user-001",
                Date = DateTime.UtcNow.AddDays(-5),
                Products = new List<CartProduct>
                {
                    new CartProduct { ProductId = 1, Quantity = 1 },
                    new CartProduct { ProductId = 3, Quantity = 1 }
                }
            },
            new Cart
            {
                Id = 2,
                UserId = "user-001",
                Date = DateTime.UtcNow.AddDays(-3),
                Products = new List<CartProduct>
                {
                    new CartProduct { ProductId = 6, Quantity = 2 },
                    new CartProduct { ProductId = 7, Quantity = 1 }
                }
            },
            new Cart
            {
                Id = 3,
                UserId = "user-002",
                Date = DateTime.UtcNow.AddDays(-7),
                Products = new List<CartProduct>
                {
                    new CartProduct { ProductId = 11, Quantity = 1 },
                    new CartProduct { ProductId = 12, Quantity = 1 }
                }
            },
            new Cart
            {
                Id = 4,
                UserId = "user-002",
                Date = DateTime.UtcNow.AddDays(-2),
                Products = new List<CartProduct>
                {
                    new CartProduct { ProductId = 16, Quantity = 1 },
                    new CartProduct { ProductId = 17, Quantity = 1 }
                }
            },
            new Cart
            {
                Id = 5,
                UserId = "user-003",
                Date = DateTime.UtcNow.AddDays(-10),
                Products = new List<CartProduct>
                {
                    new CartProduct { ProductId = 2, Quantity = 1 },
                    new CartProduct { ProductId = 5, Quantity = 1 }
                }
            },
            new Cart
            {
                Id = 6,
                UserId = "user-003",
                Date = DateTime.UtcNow.AddDays(-1),
                Products = new List<CartProduct>
                {
                    new CartProduct { ProductId = 19, Quantity = 1 },
                    new CartProduct { ProductId = 20, Quantity = 2 }
                }
            },
            new Cart
            {
                Id = 7,
                UserId = "user-001",
                Date = DateTime.UtcNow.AddDays(-15),
                Products = new List<CartProduct>
                {
                    new CartProduct { ProductId = 4, Quantity = 1 },
                    new CartProduct { ProductId = 15, Quantity = 2 }
                }
            },
            new Cart
            {
                Id = 8,
                UserId = "user-002",
                Date = DateTime.UtcNow.AddDays(-20),
                Products = new List<CartProduct>
                {
                    new CartProduct { ProductId = 8, Quantity = 1 },
                    new CartProduct { ProductId = 9, Quantity = 1 }
                }
            },
            new Cart
            {
                Id = 9,
                UserId = "user-003",
                Date = DateTime.UtcNow.AddDays(-12),
                Products = new List<CartProduct>
                {
                    new CartProduct { ProductId = 13, Quantity = 1 },
                    new CartProduct { ProductId = 14, Quantity = 1 }
                }
            },
            new Cart
            {
                Id = 10,
                UserId = "user-001",
                Date = DateTime.UtcNow,
                Products = new List<CartProduct>
                {
                    new CartProduct { ProductId = 10, Quantity = 1 },
                    new CartProduct { ProductId = 18, Quantity = 1 }
                }
            }
        };

        public IEnumerable<Cart> GetAll() => _carts.Values.OrderBy(c => c.Id);

        public Cart GetById(int id) => _carts.TryGetValue(id, out var cart) ? cart : null;

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
            cart.Id = newId;
            _carts.TryAdd(cart.Id, cart);
            return cart;
        }

        public Cart Update(int id, Cart cart)
        {
            if (!_carts.ContainsKey(id))
                return null;

            cart.Id = id;
            _carts[id] = cart;
            return cart;
        }

        public Cart Delete(int id)
        {
            _carts.TryRemove(id, out var deleted);
            return deleted;
        }
    }
}
