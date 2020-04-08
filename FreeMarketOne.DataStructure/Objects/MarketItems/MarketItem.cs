using FreeMarketOne.Utils.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.MarketItems
{
    public class MarketItem : IMarketItem
    {
        [JsonProperty("_nt")]
        public string nametype { get; set; }

        [JsonProperty("t")]
        public int Title { get; set; }

        [JsonProperty("h")]
        public string Hash { get; set; }

        [JsonProperty("d")]
        public string Description { get; set; }

        [JsonProperty("c")]
        public DateTime CreatedUtc { get; set; }

        public virtual bool IsValid ()
        {
            var content = new StringBuilder();
            var sha512processor = new Sha512Processor();

            content.Append(nametype);
            content.Append(Title);
            content.Append(Description);
            content.Append(CreatedUtc);

            var hash = sha512processor.GetSHA512(content.ToString());
            if (hash == Hash)
            {
                return true;
            } else {
                return false;
            }
        }
    }
}
