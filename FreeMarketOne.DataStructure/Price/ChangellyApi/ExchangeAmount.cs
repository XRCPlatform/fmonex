using Newtonsoft.Json;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class ExchangeAmount
    {
        /// <summary>
        /// currency to exchange from
        /// </summary>
        [JsonProperty("from")]
        public string From { get; set; }

        /// <summary>
        /// currency to exchange for
        /// </summary>
        [JsonProperty("to")]
        public string To { get; set; }

        /// <summary>
        /// commission that is taken by the network from the amount sent to the user
        /// </summary>
        [JsonProperty("networkFee")]
        public decimal NetworkFee { get; set; }

        /// <summary>
        /// amount of currency you are going to send
        /// </summary>
        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// amount that includes exchange fee
        /// </summary>
        [JsonProperty("result")]
        public decimal Result { get; set; }

        /// <summary>
        /// the amount before any fees are deducted
        /// </summary>
        [JsonProperty("visibleAmount")]
        public decimal VisibleAmount { get; set; }

        /// <summary>
        ///current rate of exchange
        /// </summary>
        [JsonProperty("rate")]
        public decimal Rate { get; set; }
        /// <summary>
        /// exchange fee
        /// </summary>
        [JsonProperty("fee")]
        public decimal Fee { get; set; }

    }

}
