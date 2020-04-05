using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price.ChangellyApi
{
    public class ValidateAddressResponse
    {
        [JsonProperty("jsonrpc")]
        public string jsonrpc { get; set; }

        [JsonProperty("id")]
        public string id { get; set; }
        
        [JsonProperty("result")]
        public Result ValidationResult { get; set; }
    }

    public class Result
    {
        [JsonProperty("result")]
        public bool IsValid{ get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

}
