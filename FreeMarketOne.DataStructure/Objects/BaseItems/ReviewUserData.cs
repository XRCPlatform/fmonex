﻿using FreeMarketOne.Utils.Security;
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

        /// <summary>
        /// This value is equal to UserData Signature of revieved user
        /// </summary>
        [JsonProperty("u")]
        public string UserSignature { get; set; }

        /// <summary>
        /// Connection to Market Item by Market Item hash
        /// </summary>
        [JsonProperty("m")]
        public string MarketItemHash { get; set; }

        public override string GenerateHash()
        {
            var content = new StringBuilder();
            var sha512processor = new Sha512Processor();

            content.Append(nametype);
            content.Append(Message);
            content.Append(UserSignature);
            content.Append(MarketItemHash);
            content.Append(ReviewDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));

            return sha512processor.GetSHA512(content.ToString());
        }

        public override byte[] ToByteArrayForSign()
        {
            var content = new StringBuilder();
            content.Append(nametype);
            content.Append(Message);
            content.Append(UserSignature);
            content.Append(MarketItemHash);
            content.Append(ReviewDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));

            return Encoding.ASCII.GetBytes(content.ToString());
        }
    }
}
