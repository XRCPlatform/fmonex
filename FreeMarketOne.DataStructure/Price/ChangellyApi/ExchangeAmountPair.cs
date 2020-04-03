using Newtonsoft.Json;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class ExchangeAmountPair
    {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("networkFee")]
        public double NetworkFee { get; set; }

        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("result")]
        public double Result { get; set; }

        [JsonProperty("visibleAmount")]
        public double VisibleAmount { get; set; }

        [JsonProperty("rate")]
        public double Rate { get; set; }

        [JsonProperty("fee")]
        public double Fee { get; set; }

    }

}
