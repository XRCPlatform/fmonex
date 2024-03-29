﻿using Libplanet.Extensions;
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
        /// This value is equal to UserData Signature of reviever user
        /// </summary>
        [JsonProperty("u")]
        public string UserSignature { get; set; }

        /// <summary>
        /// This value is equal to UserData Hash of reviever user
        /// </summary>
        [JsonProperty("r")]
        public string UserHash { get; set; }

        /// <summary>
        /// Connection to Market Item by Market Item hash (probably for nothing but...)
        /// </summary>
        [JsonProperty("i")]
        public string MarketItemHash { get; set; }

        /// <summary>
        /// User who recieved a review public key
        /// </summary>
        [JsonProperty("k")]
        public string RevieweePublicKey { get; set; }

        /// <summary>
        /// User stars
        /// </summary>
        [JsonProperty("t")]
        public int Stars { get; set; }

        /* Rendering Helpers */
        [JsonIgnore]
        public virtual string UserName { get; set; }

        [JsonIgnore]
        public virtual string UserSignatureAndHash { get; set; }

        public override string GenerateHash()
        {
            var content = new StringBuilder();
            var shaProcessor = new SHAProcessor();

            content.Append(nametype);
            content.Append(Message);
            content.Append(UserSignature);
            content.Append(UserHash);
            content.Append(MarketItemHash);
            content.Append(Stars);
            content.Append(ReviewDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
            content.Append(RevieweePublicKey);

            return shaProcessor.GetSHA256(content.ToString());
        }

        public override byte[] ToByteArrayForSign()
        {
            var content = new StringBuilder();
            content.Append(nametype);
            content.Append(Message);
            content.Append(UserSignature);
            content.Append(UserHash);
            content.Append(MarketItemHash);
            content.Append(Stars);
            content.Append(ReviewDateTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
            content.Append(RevieweePublicKey);

            return Encoding.ASCII.GetBytes(content.ToString());
        }
    }
}
