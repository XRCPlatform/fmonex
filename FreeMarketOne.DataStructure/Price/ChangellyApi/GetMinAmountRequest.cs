using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class GetMinAmountRequest
    {
        public GetMinAmountRequest(Currency from, Currency[] to)
        {
            SimplePair[] pairs = new SimplePair[to.Length];
            for (int i = 0; i < to.Length; i++)
            {
                pairs[i] = new SimplePair()
                {
                    From = from.ToString().ToLower(),
                    To = to[i].ToString().ToLower()
                };
            }           
            this.Parameters = pairs;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("jsonrpc")]
        public const string jsonrpc = "2.0";

        [JsonProperty("method")]
        public const string method = "getMinAmount";

        [JsonProperty("params")]
        public SimplePair[] Parameters { get; set; }
        
    }

    public class SimplePair
    {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }
    }


}
