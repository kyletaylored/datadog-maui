using System;
using System.Linq;
using System.Web.Http;
using Datadog.Trace;
using DatadogMauiApi.Framework.Models;
using DatadogMauiApi.Framework.Services;

namespace DatadogMauiApi.Framework.Controllers
{
    [RoutePrefix("carts")]
    public class CartsController : DatadogApiController
    {
        private readonly CartStore _store;
        private readonly SessionManager _sessionManager;

        public CartsController()
        {
            _store = CartStore.Instance;
            _sessionManager = SessionManager.Instance;
        }

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllCarts(int? limit = null, string sort = "asc",
            string startdate = null, string enddate = null)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "GET /carts";
                span.SetTag("custom.operation.type", "carts_list");
                span.SetTag("custom.query.limit", limit?.ToString() ?? "none");
                span.SetTag("custom.query.sort", sort);
            }

            var carts = _store.GetAll();

            // Date filtering if provided
            if (!string.IsNullOrEmpty(startdate) || !string.IsNullOrEmpty(enddate))
            {
                DateTime? start = string.IsNullOrEmpty(startdate) ? (DateTime?)null : DateTime.Parse(startdate);
                DateTime? end = string.IsNullOrEmpty(enddate) ? (DateTime?)null : DateTime.Parse(enddate);
                carts = _store.GetByDateRange(start, end);

                if (span != null)
                {
                    span.SetTag("custom.query.startdate", startdate ?? "none");
                    span.SetTag("custom.query.enddate", enddate ?? "none");
                }
            }

            // Apply sorting
            if (sort == "desc")
                carts = carts.OrderByDescending(c => c.Id);

            // Apply limit
            if (limit.HasValue && limit > 0)
                carts = carts.Take(limit.Value);

            var result = carts.ToList();

            if (span != null)
                span.SetTag("custom.result.count", result.Count.ToString());

            return Ok(result);
        }

        [HttpGet]
        [Route("{id:int}")]
        public IHttpActionResult GetCart(int id)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "GET /carts/{id}";
                span.SetTag("custom.operation.type", "cart_get");
                span.SetTag("custom.cart.id", id.ToString());
            }

            var cart = _store.GetById(id);

            if (cart == null)
            {
                if (span != null)
                    span.SetTag("custom.cart.found", "false");
                return NotFound();
            }

            if (span != null)
            {
                span.SetTag("custom.cart.found", "true");
                span.SetTag("custom.cart.product_count", cart.Products.Count.ToString());
            }

            return Ok(cart);
        }

        [HttpGet]
        [Route("user/{userId}")]
        public IHttpActionResult GetCartsByUser(string userId)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "GET /carts/user/{userId}";
                span.SetTag("custom.operation.type", "carts_by_user");
                span.SetTag("custom.user.id", userId);
            }

            var carts = _store.GetByUserId(userId).ToList();

            if (span != null)
                span.SetTag("custom.result.count", carts.Count.ToString());

            return Ok(carts);
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult CreateCart([FromBody] Cart cart)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "POST /carts";
                span.SetTag("custom.operation.type", "cart_create");
                span.SetTag("custom.cart.user_id", cart.UserId);
                span.SetTag("custom.cart.product_count", cart.Products.Count.ToString());
            }

            if (cart == null)
            {
                return BadRequest("Cart data is required");
            }

            var created = _store.Add(cart);

            if (span != null)
                span.SetTag("custom.cart.id", created.Id.ToString());

            return Ok(created);
        }

        [HttpPut]
        [Route("{id:int}")]
        public IHttpActionResult UpdateCart(int id, [FromBody] Cart cart)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "PUT /carts/{id}";
                span.SetTag("custom.operation.type", "cart_update");
                span.SetTag("custom.cart.id", id.ToString());
            }

            if (cart == null)
            {
                return BadRequest("Cart data is required");
            }

            var updated = _store.Update(id, cart);

            if (updated == null)
            {
                if (span != null)
                    span.SetTag("custom.cart.found", "false");
                return NotFound();
            }

            if (span != null)
                span.SetTag("custom.cart.found", "true");

            return Ok(updated);
        }

        [HttpPatch]
        [Route("{id:int}")]
        public IHttpActionResult PatchCart(int id, [FromBody] Cart cart)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "PATCH /carts/{id}";
                span.SetTag("custom.operation.type", "cart_patch");
                span.SetTag("custom.cart.id", id.ToString());
            }

            if (cart == null)
            {
                return BadRequest("Cart data is required");
            }

            var updated = _store.Update(id, cart);

            if (updated == null)
            {
                if (span != null)
                    span.SetTag("custom.cart.found", "false");
                return NotFound();
            }

            if (span != null)
                span.SetTag("custom.cart.found", "true");

            return Ok(updated);
        }

        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult DeleteCart(int id)
        {
            var span = GetDatadogSpan();
            if (span != null)
            {
                span.ResourceName = "DELETE /carts/{id}";
                span.SetTag("custom.operation.type", "cart_delete");
                span.SetTag("custom.cart.id", id.ToString());
            }

            var deleted = _store.Delete(id);

            if (deleted == null)
            {
                if (span != null)
                    span.SetTag("custom.cart.found", "false");
                return NotFound();
            }

            if (span != null)
                span.SetTag("custom.cart.found", "true");

            return Ok(deleted);
        }
    }
}
