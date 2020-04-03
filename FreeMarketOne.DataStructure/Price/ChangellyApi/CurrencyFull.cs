using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class CurrencyFull
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("ticker")]
        public string Ticker { get; set; }
        [JsonProperty("fullName")]
        public string FullName { get; set; }
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
        [JsonProperty("fixRateEnabled")]
        public bool FixRateEnabled { get; set; }
        [JsonProperty("payinConfirmations")]
        public int PayinConfirmations { get; set; }
        [JsonProperty("extraIdName")]
        public object ExtraIdName { get; set; }
        [JsonProperty("addressUrl")]
        public string AddressUrl { get; set; }
        [JsonProperty("transactionUrl")]
        public string TransactionUrl { get; set; }
        [JsonProperty("image")]
        public string Image { get; set; }
        [JsonProperty("fixedTime")]
        public int FixedTime { get; set; }

    }
}
