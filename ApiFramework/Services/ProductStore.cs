using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DatadogMauiApi.Framework.Models;

namespace DatadogMauiApi.Framework.Services
{
    public class ProductStore
    {
        private static readonly Lazy<ProductStore> _instance = new Lazy<ProductStore>(() => new ProductStore());
        public static ProductStore Instance => _instance.Value;

        private readonly ConcurrentDictionary<int, Product> _products;
        private int _nextId = 21; // Start after seed data

        private ProductStore()
        {
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
        }

        private static List<Product> GetSeedProducts() => new List<Product>
        {
            // Electronics (5 items)
            new Product { Id = 1, Title = "Laptop", Price = 799.99m, Description = "High-performance laptop with 16GB RAM", Image = "https://example.com/laptop.jpg", Category = "electronics" },
            new Product { Id = 2, Title = "Smartphone", Price = 699.99m, Description = "Latest model smartphone with 128GB storage", Image = "https://example.com/phone.jpg", Category = "electronics" },
            new Product { Id = 3, Title = "Wireless Headphones", Price = 149.99m, Description = "Noise-cancelling wireless headphones", Image = "https://example.com/headphones.jpg", Category = "electronics" },
            new Product { Id = 4, Title = "Tablet", Price = 449.99m, Description = "10-inch tablet with stylus support", Image = "https://example.com/tablet.jpg", Category = "electronics" },
            new Product { Id = 5, Title = "Smart Watch", Price = 299.99m, Description = "Fitness tracking smart watch", Image = "https://example.com/watch.jpg", Category = "electronics" },

            // Clothing (5 items)
            new Product { Id = 6, Title = "T-Shirt", Price = 19.99m, Description = "Cotton t-shirt in various colors", Image = "https://example.com/tshirt.jpg", Category = "clothing" },
            new Product { Id = 7, Title = "Jeans", Price = 49.99m, Description = "Classic fit denim jeans", Image = "https://example.com/jeans.jpg", Category = "clothing" },
            new Product { Id = 8, Title = "Jacket", Price = 89.99m, Description = "All-weather jacket with hood", Image = "https://example.com/jacket.jpg", Category = "clothing" },
            new Product { Id = 9, Title = "Sneakers", Price = 79.99m, Description = "Comfortable running sneakers", Image = "https://example.com/sneakers.jpg", Category = "clothing" },
            new Product { Id = 10, Title = "Hat", Price = 24.99m, Description = "Adjustable baseball cap", Image = "https://example.com/hat.jpg", Category = "clothing" },

            // Home & Garden (5 items)
            new Product { Id = 11, Title = "Coffee Maker", Price = 89.99m, Description = "Programmable coffee maker with timer", Image = "https://example.com/coffee.jpg", Category = "home" },
            new Product { Id = 12, Title = "Blender", Price = 59.99m, Description = "High-speed blender for smoothies", Image = "https://example.com/blender.jpg", Category = "home" },
            new Product { Id = 13, Title = "Vacuum Cleaner", Price = 199.99m, Description = "Cordless vacuum with HEPA filter", Image = "https://example.com/vacuum.jpg", Category = "home" },
            new Product { Id = 14, Title = "Garden Tools Set", Price = 49.99m, Description = "Complete set of gardening tools", Image = "https://example.com/tools.jpg", Category = "home" },
            new Product { Id = 15, Title = "Throw Pillow", Price = 29.99m, Description = "Decorative throw pillow", Image = "https://example.com/pillow.jpg", Category = "home" },

            // Sports & Outdoors (5 items)
            new Product { Id = 16, Title = "Yoga Mat", Price = 34.99m, Description = "Non-slip yoga mat with carrying strap", Image = "https://example.com/yoga.jpg", Category = "sports" },
            new Product { Id = 17, Title = "Dumbbell Set", Price = 99.99m, Description = "Adjustable dumbbell set 5-50 lbs", Image = "https://example.com/dumbbells.jpg", Category = "sports" },
            new Product { Id = 18, Title = "Camping Tent", Price = 149.99m, Description = "4-person waterproof camping tent", Image = "https://example.com/tent.jpg", Category = "sports" },
            new Product { Id = 19, Title = "Bicycle", Price = 399.99m, Description = "Mountain bike with 21 speeds", Image = "https://example.com/bike.jpg", Category = "sports" },
            new Product { Id = 20, Title = "Soccer Ball", Price = 24.99m, Description = "Official size soccer ball", Image = "https://example.com/soccer.jpg", Category = "sports" }
        };

        public IEnumerable<Product> GetAll() => _products.Values.OrderBy(p => p.Id);

        public Product GetById(int id) => _products.TryGetValue(id, out var product) ? product : null;

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
            product.Id = newId;
            _products.TryAdd(product.Id, product);
            return product;
        }

        public Product Update(int id, Product product)
        {
            if (!_products.ContainsKey(id))
                return null;

            product.Id = id;
            _products[id] = product;
            return product;
        }

        public Product Delete(int id)
        {
            _products.TryRemove(id, out var deleted);
            return deleted;
        }
    }
}
