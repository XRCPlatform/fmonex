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
        public DateTimeOffset BlockDateTime { get; set; }

        public override string GenerateHash()
        {
            var content = new StringBuilder();
            var sha512processor = new Sha512Processor();

            content.Append(CreatedUtc);
            content.Append(BlockHash);
            content.Append(BlockDateTime.Ticks);

            return sha512processor.GetSHA512(content.ToString());
        }
    }
}
