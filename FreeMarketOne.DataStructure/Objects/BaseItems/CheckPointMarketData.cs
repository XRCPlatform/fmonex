using Libplanet.Blocks;
using Libplanet.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    public class CheckPointMarketData : BaseItem
    {
        [JsonProperty("b")]
        public string Block { get; set; }

        public override string GenerateHash()
        {
            var content = new StringBuilder();
            var shaProcessor = new SHAProcessor();

            content.Append(nametype);
            content.Append(Block);
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));

            return shaProcessor.GetSHA256(content.ToString());
        }

        public override byte[] ToByteArrayForSign()
        {
            var content = new StringBuilder();
            content.Append(nametype);
            content.Append(Block);
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));

            return Encoding.ASCII.GetBytes(content.ToString());
        }
    }
}
