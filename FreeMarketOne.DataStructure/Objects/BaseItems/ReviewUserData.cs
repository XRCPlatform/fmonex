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

        public override bool IsValid()
        {
            var content = new StringBuilder();
            var sha512processor = new Sha512Processor();

            content.Append(Version);
            content.Append(Message);
            content.Append(CreatedUtc);
            content.Append(ReviewDateTime.Ticks);

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
