using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DatadogMauiApi.Framework.Models
{
    public class Cart
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("products")]
        public List<CartProduct> Products { get; set; }
    }
}
