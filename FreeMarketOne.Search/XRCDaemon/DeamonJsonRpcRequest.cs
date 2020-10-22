using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FreeMarketOne.Search.XRCDaemon.JsonRpc
{

    [JsonObject(MemberSerialization.OptIn)]
    public class DeamonJsonRpcRequest<T>
    {
        public DeamonJsonRpcRequest()
        {
        }

        public DeamonJsonRpcRequest(string method, T parameters, object id)
        {
            Method = method;
            Params = parameters;
            Id = id;
        }

        [JsonProperty("method", NullValueHandling = NullValueHandling.Ignore)]
        public string Method { get; set; }

        [JsonProperty("params")]
        public object Params { get; set; }

        [JsonProperty("id")]
        public object Id { get; set; }

        public TParam ParamsAs<TParam>() where TParam : class
        {
            if (Params is JToken)
                return ((JToken) Params)?.ToObject<TParam>();

            return (TParam) Params;
        }
    }
}
