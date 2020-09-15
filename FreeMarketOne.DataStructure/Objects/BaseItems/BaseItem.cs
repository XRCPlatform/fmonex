using Libplanet.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    public class BaseItem : IBaseItem
    {
        public BaseItem()
        {
            CreatedUtc = DateTime.UtcNow;
        }

        [JsonProperty("_nt")]
        public string nametype { get; set; }

        [JsonProperty("h")]
        public string Hash { get; set; }

        [JsonProperty("c")]
        public DateTime CreatedUtc { get; set; }

        [JsonProperty("s")]
        public string Signature { get; set; }

        public virtual bool IsValid()
        {
            if (GenerateHash() == Hash)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual string GenerateHash()
        {
            var content = new StringBuilder();
            var shaProcessor = new SHAProcessor();

            content.Append(CreatedUtc);

            return shaProcessor.GetSHA256(content.ToString());
        }

        public virtual byte[] ToByteArrayForSign()
        {
            var content = new StringBuilder();
            content.Append(nametype);
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));

            return Encoding.ASCII.GetBytes(content.ToString());
        }
    }
}
