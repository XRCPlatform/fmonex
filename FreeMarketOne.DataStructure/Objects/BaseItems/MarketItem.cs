﻿using Avalonia.Media.Imaging;
using FreeMarketOne.Utils.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    public class MarketItem : IBaseItem
    {
        [JsonProperty("_nt")]
        public string nametype { get; set; }

        [JsonProperty("t")]
        public string Title { get; set; }

        [JsonProperty("d")]
        public string Description { get; set; }

        [JsonProperty("s")]
        public string Shipping { get; set; }

        [JsonProperty("x")]
        public int DealType { get; set; }

        [JsonProperty("k")]
        public int Category { get; set; }

        [JsonProperty("p")]
        public float Price { get; set; }

        [JsonProperty("r")]
        public int PriceType { get; set; }

        [JsonProperty("i")]
        public List<string> Photos { get; set; }

        [JsonProperty("b")]
        public string BaseSignature { get; set; }

        [JsonProperty("u")]
        public string BuyerSignature { get; set; }

        [JsonProperty("c")]
        public DateTime CreatedUtc { get; set; }

        [JsonProperty("s")]
        public string Signature { get; set; }

        [JsonProperty("h")]
        public string Hash { get; set; }

        /* Rendering Helpers */
        [JsonIgnore]
        public List<Bitmap> PrePhotos { get; set; }

        [JsonIgnore]
        public Bitmap PreTitlePhoto { get; set; }

        public MarketItem()
        {
            this.Photos = new List<string>();
            this.CreatedUtc = DateTime.UtcNow;
        }

        public virtual bool IsValid()
        {
            if (GenerateHash() == Hash)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual string GenerateHash()
        {
            var content = new StringBuilder();
            var sha512processor = new Sha512Processor();

            content.Append(nametype);
            content.Append(Title);
            content.Append(Description);
            content.Append(Shipping);
            content.Append(DealType);
            content.Append(Category);
            content.Append(Price);
            content.Append(PriceType);
            content.Append(string.Join(string.Empty, Photos.ToArray()));
            content.Append(BaseSignature);
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));

            return sha512processor.GetSHA512(content.ToString());
        }

        public virtual byte[] ToByteArrayForSign()
        {
            var content = new StringBuilder();
            content.Append(nametype);
            content.Append(Title);
            content.Append(Description);
            content.Append(Shipping);
            content.Append(DealType);
            content.Append(Category);
            content.Append(Price);
            content.Append(PriceType);
            content.Append(string.Join(string.Empty, Photos.ToArray()));
            content.Append(BaseSignature);
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));

            return Encoding.ASCII.GetBytes(content.ToString());
        }
    }
}