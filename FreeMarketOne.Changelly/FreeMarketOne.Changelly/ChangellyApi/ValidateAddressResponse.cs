using Newtonsoft.Json;

namespace FreeMarketOne.Changelly
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
