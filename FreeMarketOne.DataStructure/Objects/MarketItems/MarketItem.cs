using FreeMarketOne.Utils.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.MarketItems
{
    public class MarketItem : IMarketItem
    {
        [JsonProperty("_nt")]
        public string nametype { get; set; }

        [JsonProperty("t")]
        public string Title { get; set; }

        [JsonProperty("h")]
        public string Hash { get; set; }

        [JsonProperty("d")]
        public string Description { get; set; }

        [JsonProperty("c")]
        public DateTime CreatedUtc { get; set; }

        [JsonProperty("s")]
        public string Shipping { get; set; }

        [JsonProperty("x")]
        public string DealType { get; set; }

        [JsonProperty("c")]
        public string Category { get; set; }

        [JsonProperty("p")]
        public List<string> Photos { get; set; }

        public MarketItem()
        {
            this.Photos = new List<string>();
        }

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

            content.Append(nametype);
            content.Append(Title);
            content.Append(Description);
            content.Append(Shipping);
            content.Append(DealType);
            content.Append(Category);
            content.Append(String.Join(String.Empty, Photos.ToArray()));

            return sha512processor.GetSHA512(content.ToString());
        }
    }
}
