using FreeMarketOne.Utils.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    public class UserData : BaseItem
    {
        [JsonProperty("t")]
        public string UserName { get; set; }

        [JsonProperty("d")]
        public string Description { get; set; }

        [JsonProperty("p")]
        public string Photo { get; set; }

        [JsonProperty("b")]
        public string BaseSignature { get; set; }

        public override string GenerateHash()
        {
            var content = new StringBuilder();
            var sha512processor = new Sha512Processor();

            content.Append(nametype);
            content.Append(UserName);
            content.Append(Description);
            content.Append(Photo);
            content.Append(BaseSignature);
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));

            return sha512processor.GetSHA512(content.ToString());
        }

        public override byte[] ToByteArrayForSign()
        {
            var content = new StringBuilder();
            content.Append(nametype);
            content.Append(UserName);
            content.Append(Description);
            content.Append(Photo);
            content.Append(BaseSignature);
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));

            return Encoding.ASCII.GetBytes(content.ToString());
        }
    }
}
