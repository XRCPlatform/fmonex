using FreeMarketOne.Utils.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    public class CheckPointMarketData : BaseItem
    {
        [JsonProperty("b")]
        public string BlockHash { get; set; }

        [JsonProperty("d")]
        public DateTime BlockDateTime { get; set; }

        public override bool IsValid()
        {
            var content = new StringBuilder();
            var sha512processor = new Sha512Processor();

            content.Append(Version);
            content.Append(BlockHash);
            content.Append(CreatedUtc);
            content.Append(BlockHash);
            content.Append(BlockDateTime.Ticks);

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
