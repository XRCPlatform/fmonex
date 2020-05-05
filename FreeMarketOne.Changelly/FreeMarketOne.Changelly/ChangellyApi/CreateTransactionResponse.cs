using Newtonsoft.Json;
using System;

namespace FreeMarketOne.Changelly
{
    public class CreateTransactionResponse
    {
        [JsonProperty("jsonrpc")]
        public string jsonrpc { get; set; }

        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("result")]
        public ChangellyTransaction InitiatedTransaction { get; set; }
    }

    public class ChangellyTransaction
    {
        [JsonProperty("id")]
        public string id { get; set; }

        [JsonProperty("apiExtraFee")]
        public decimal apiExtraFee { get; set; }

        [JsonProperty("changellyFee")]
        public decimal changellyFee { get; set; }
        
        [JsonProperty("payinExtraId")]
        public string payinExtraId { get; set; }

        [JsonProperty("amountExpectedFrom")]
        public decimal amountExpectedFrom { get; set; }

        [JsonProperty("status")]
        public string status { get; set; }

        [JsonProperty("currencyFrom")]
        public string currencyFrom { get; set; }

        [JsonProperty("currencyTo")]
        public string currencyTo { get; set; }

        [JsonProperty("amountTo")]
        public decimal amountTo { get; set; }

        [JsonProperty("amountExpectedTo")]
        public decimal amountExpectedTo { get; set; }

        [JsonProperty("payinAddress")]
        public string payinAddress { get; set; }

        [JsonProperty("payoutAddress")]
        public string payoutAddress { get; set; }

        [JsonProperty("createdAt")]
        public DateTime createdAt { get; set; }

        [JsonProperty("redirect")]
        public string redirect { get; set; }

        [JsonProperty("kycRequired")]
        public bool kycRequired { get; set; }

        [JsonProperty("signature")]
        public string signature { get; set; }

        [JsonProperty("binaryPayload")]
        public string binaryPayload { get; set; }
    }

}
