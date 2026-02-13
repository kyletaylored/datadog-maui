using Newtonsoft.Json;

namespace DatadogMauiApi.Framework.Models
{
    public class CartProduct
    {
        [JsonProperty("productId")]
        public int ProductId { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }
    }
}
