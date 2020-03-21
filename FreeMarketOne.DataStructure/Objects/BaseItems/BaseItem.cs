using FreeMarketOne.Utils.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    public class BaseItem : IBaseItem
    {
        [JsonProperty("v")]
        public int Version { get; set; }

        [JsonProperty("h")]
        public string Hash { get; set; }

        [JsonProperty("c")]
        public DateTime CreatedUtc { get; set; }

        public virtual bool IsValid()
        {
            var content = new StringBuilder();
            var sha512processor = new Sha512Processor();

            content.Append(Version);
            content.Append(CreatedUtc);

            var hash = sha512processor.GetSHA512(content.ToString());
            if (hash == Hash)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
