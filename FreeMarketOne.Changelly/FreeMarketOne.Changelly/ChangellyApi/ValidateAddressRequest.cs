using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Changelly
{
     public class ValidateAddressRequest
    {
        public ValidateAddressRequest(string currency, string address)
        {
            ValidatableCurrencyAddressPair = new  CurrencyAddressPair(currency, address);
        }

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("jsonrpc")]
        public const string jsonrpc = "2.0";

        [JsonProperty("method")]
        public const string method = "validateAddress";

        [JsonProperty("params")]
        public CurrencyAddressPair ValidatableCurrencyAddressPair { get; set; }
    }

    public class CurrencyAddressPair
    {
        public CurrencyAddressPair(string currency, string address)
        {
            Currency = currency;
            Address = address;
        }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }
    }
}
