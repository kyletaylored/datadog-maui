using System.Linq;
using System.Web.Http;
using Datadog.Trace;
using DatadogMauiApi.Framework.Models;
using DatadogMauiApi.Framework.Services;

namespace DatadogMauiApi.Framework.Controllers
{
    [RoutePrefix("products")]
    public class ProductsController : DatadogApiController
    {
        private readonly ProductStore _store;
        private readonly SessionManager _sessionManager;

        public ProductsController()
        {
            _store = ProductStore.Instance;
            _sessionManager = SessionManager.Instance;
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllProducts(int? limit = null, string sort = "asc")
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "GET /products";
                span.SetTag("custom.operation.type", "products_list");
                span.SetTag("custom.query.limit", limit?.ToString() ?? "none");
                span.SetTag("custom.query.sort", sort);
            }

            var products = _store.GetAll();

            // Apply sorting
            if (sort == "desc")
                products = products.OrderByDescending(p => p.Id);

            // Apply limit
            if (limit.HasValue && limit > 0)
                products = products.Take(limit.Value);

            var result = products.ToList();

            if (span != null)
                span.SetTag("custom.result.count", result.Count.ToString());

            return Ok(result);
        }

        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetProduct(int id)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "GET /products/{id}";
                span.SetTag("custom.operation.type", "product_get");
                span.SetTag("custom.product.id", id.ToString());
            }

            var product = _store.GetById(id);

            if (product == null)
            {
                if (span != null)
                    span.SetTag("custom.product.found", "false");
                return NotFound();
            }

            if (span != null)
                span.SetTag("custom.product.found", "true");

            return Ok(product);
        }

        [HttpGet]
        [Route("categories")]
        public IHttpActionResult GetCategories()
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "GET /products/categories";
                span.SetTag("custom.operation.type", "categories_list");
            }

            var categories = _store.GetCategories().ToList();

            if (span != null)
                span.SetTag("custom.result.count", categories.Count.ToString());

            return Ok(categories);
        }

        [HttpGet]
        [Route("category/{category}")]
        public IHttpActionResult GetProductsByCategory(string category, int? limit = null, string sort = "asc")
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "GET /products/category/{category}";
                span.SetTag("custom.operation.type", "products_by_category");
                span.SetTag("custom.category", category);
                span.SetTag("custom.query.limit", limit?.ToString() ?? "none");
                span.SetTag("custom.query.sort", sort);
            }

            var products = _store.GetByCategory(category);

            // Apply sorting
            if (sort == "desc")
                products = products.OrderByDescending(p => p.Id);

            // Apply limit
            if (limit.HasValue && limit > 0)
                products = products.Take(limit.Value);

            var result = products.ToList();

            if (span != null)
                span.SetTag("custom.result.count", result.Count.ToString());

            return Ok(result);
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateProduct([FromBody] Product product)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "POST /products";
                span.SetTag("custom.operation.type", "product_create");
                span.SetTag("custom.product.category", product.Category);
            }

            if (product == null)
            {
                return BadRequest("Product data is required");
            }

            // Optional: Check authentication
            var authHeader = Request.Headers.Authorization;
            if (authHeader != null && !string.IsNullOrEmpty(authHeader.Parameter))
            {
                var validation = _sessionManager.ValidateSession(authHeader.Parameter);
                if (validation.Item1 && validation.Item2 != null && span != null)
                {
                    span.SetTag("custom.user.id", validation.Item2);
                    span.SetTag("custom.authenticated", "true");
                }
            }

            var created = _store.Add(product);

            if (span != null)
                span.SetTag("custom.product.id", created.Id.ToString());

            return Ok(created);
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult UpdateProduct(int id, [FromBody] Product product)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "PUT /products/{id}";
                span.SetTag("custom.operation.type", "product_update");
                span.SetTag("custom.product.id", id.ToString());
            }

            if (product == null)
            {
                return BadRequest("Product data is required");
            }

            var updated = _store.Update(id, product);

            if (updated == null)
            {
                if (span != null)
                    span.SetTag("custom.product.found", "false");
                return NotFound();
            }

            if (span != null)
                span.SetTag("custom.product.found", "true");

            return Ok(updated);
        }

        [HttpPatch]
        [Route("{id:int}")]
        public IHttpActionResult PatchProduct(int id, [FromBody] Product product)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "PATCH /products/{id}";
                span.SetTag("custom.operation.type", "product_patch");
                span.SetTag("custom.product.id", id.ToString());
            }

            if (product == null)
            {
                return BadRequest("Product data is required");
            }

            var updated = _store.Update(id, product);

            if (updated == null)
            {
                if (span != null)
                    span.SetTag("custom.product.found", "false");
                return NotFound();
            }

            if (span != null)
                span.SetTag("custom.product.found", "true");

            return Ok(updated);
        }

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult DeleteProduct(int id)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "DELETE /products/{id}";
                span.SetTag("custom.operation.type", "product_delete");
                span.SetTag("custom.product.id", id.ToString());
            }

            var deleted = _store.Delete(id);

            if (deleted == null)
            {
                if (span != null)
                    span.SetTag("custom.product.found", "false");
                return NotFound();
            }

            if (span != null)
                span.SetTag("custom.product.found", "true");

            return Ok(deleted);
        }
    }
}
