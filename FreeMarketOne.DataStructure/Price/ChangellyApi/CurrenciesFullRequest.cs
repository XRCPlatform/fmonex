using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class CurrenciesFullRequest
    {
        public CurrenciesFullRequest()
        {
            this.Parameters = new object();
        }

        [JsonProperty("jsonrpc")]
        public const string jsonrpc = "2.0";
        [JsonProperty("method")]
        public const string method = "getCurrenciesFull";
        [JsonProperty("params")]
        public object Parameters { get; set; }
        [JsonProperty("id")]
        public int Id { get; set; }
    }

}
