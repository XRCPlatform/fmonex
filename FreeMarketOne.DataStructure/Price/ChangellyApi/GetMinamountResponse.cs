using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{

    public class GetMinAmountResponse
    {
        public string jsonrpc { get; set; }
        public int id { get; set; }
        public GetMinamountResponseResult[] result { get; set; }
    }

    public class GetMinamountResponseResult
    {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("minAmount")]
        public decimal MinAmount { get; set; }
    }

}
