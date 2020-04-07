using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class CreateTransactionRequest
    {
        public CreateTransactionRequest(string from, string to, string address, string refundAddress, decimal amount)
        {
            Params = new CreateTransactionRequestParams()
            {
                From = from,
                To = to,
                Address = address,
                RefundAddress = refundAddress,
                Amount = amount
            };
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("jsonrpc")]
        public const string Jsonrpc = "2.0";

        [JsonProperty("method")]
        public const string Method = "createTransaction";

        [JsonProperty("params")]
        public CreateTransactionRequestParams Params { get; set; }
    }

    public class CreateTransactionRequestParams
    {
        [JsonProperty("from")]
        public string From { get; set; }
        
        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("refundAddress")]
        public string RefundAddress { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }
    }

}
