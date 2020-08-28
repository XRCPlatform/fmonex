﻿using FreeMarketOne.Utils.Security;
using Libplanet.Blocks;
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
            var sha512processor = new Sha512Processor();

            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
            content.Append(Block);

            return sha512processor.GetSHA512(content.ToString());
        }
    }
}
