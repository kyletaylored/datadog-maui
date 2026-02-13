using Newtonsoft.Json;

namespace DatadogMauiApi.Framework.Models
{
    public class Product
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("price")]
        public decimal Price { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }
    }
}
