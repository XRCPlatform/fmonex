using FreeMarketOne.Utils.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.Item
{
    public class Item : IItem
    {
        [JsonProperty("v")]
        public int Version { get; set; }

        [JsonProperty("t")]
        public int Title { get; set; }

        [JsonProperty("h")]
        public string Hash { get; set; }

        public bool IsValid ()
        {
            var content = new StringBuilder();
            var sha512processor = new Sha512Processor();

            content.Append(Version);
            content.Append(Title);

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
