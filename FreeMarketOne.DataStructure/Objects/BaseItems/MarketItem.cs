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

        [JsonProperty("t")]
        public string Signature { get; set; }

        /// <summary>
        /// Fineness for a given material, for example "999 fine rhodium", "24 karat gold", "999.9 four nines fine silver"
        /// </summary>
        [JsonProperty("f")]
        public string Fineness { get; set; }

        /// <summary>
        /// To standardise the price per gram and etc, conversion into common metric unit.
        /// </summary>
        [JsonProperty("w")]
        public string WeightInGrams { get; set; }

        /// <summary>
        /// Bar size in commercial terms for example 1 oz, 1 troy ounce (ozt), 1 tola, 1 kg. Should be standardised so that could be used as filter.
        /// </summary>
        [JsonProperty("z")]
        public string Size { get; set; }

        /// <summary>
        /// Manufacturer or mint to produce this product. For example "Baird & Co", "The Royal Mint", "Umicore", "PAMP SA", Should be standardised so that could be used as filter.
        /// </summary>
        [JsonProperty("m")]
        public string Manufacturer { get; set; }

        [JsonProperty("h")]
        public string Hash { get; set; }

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
            StringBuilder content = GetString();
            var sha512processor = new Sha512Processor();
            return sha512processor.GetSHA512(content.ToString());
        }

        public virtual byte[] ToByteArrayForSign()
        {
            StringBuilder content = GetString();
            return Encoding.ASCII.GetBytes(content.ToString());
        }

        private StringBuilder GetString()
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
            content.Append(Fineness);
            content.Append(WeightInGrams);
            content.Append(Size);
            content.Append(Manufacturer);
            return content;
        }
    }
}
