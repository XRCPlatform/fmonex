using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class ExchangeAmountRequest
    {
        public ExchangeAmountRequest(Currency baseCurrency, Currency[] exchangeCurrencies, decimal amount)
        {
            ExchangePair[] pairs = new ExchangePair[exchangeCurrencies.Length];
            for (int i = 0; i < exchangeCurrencies.Length; i++)
            {
                pairs[i] = new ExchangePair()
                {
                    Amount = amount,
                    From = baseCurrency.ToString().ToLower(),
                    To = exchangeCurrencies[i].ToString().ToLower()
                };
            }
            this.Parameters = pairs;
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("jsonrpc")]
        public const string jsonrpc = "2.0";

        [JsonProperty("method")]
        public const string method = "getExchangeAmount";

        [JsonProperty("params")]
        public ExchangePair[] Parameters { get; set; }
 
    }

    public class ExchangePair
    {
        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }
    }
}
