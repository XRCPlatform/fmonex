using FreeMarketOne.Utils.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    public class ReviewUserData : BaseItem
    {
        [JsonProperty("m")]
        public string Message { get; set; }

        [JsonProperty("d")]
        public DateTime ReviewDateTime { get; set; }

        public override string GenerateHash()
        {
            var content = new StringBuilder();
            var sha512processor = new Sha512Processor();

            content.Append(nametype);
            content.Append(Message);
            content.Append(CreatedUtc);
            content.Append(ReviewDateTime.Ticks);

            return sha512processor.GetSHA512(content.ToString());
        }
    }
}
