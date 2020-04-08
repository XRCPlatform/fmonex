using FreeMarketOne.Utils.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    public class BaseItem : IBaseItem
    {
        [JsonProperty("_nt")]
        public string nametype { get; set; }

        [JsonProperty("h")]
        public string Hash { get; set; }

        [JsonProperty("c")]
        public DateTime CreatedUtc { get; set; }

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
            var sha512processor = new Sha512Processor();

            content.Append(CreatedUtc);

            return sha512processor.GetSHA512(content.ToString());
        }
    }
}
