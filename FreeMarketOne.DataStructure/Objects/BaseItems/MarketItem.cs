using Avalonia.Media.Imaging;
using Libplanet.Extensions;
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

        [JsonProperty("g")]
        public string Shipping { get; set; }

        [JsonProperty("x")]
        public int DealType { get; set; }

        [JsonProperty("k")]
        public int Category { get; set; }

        [JsonProperty("p")]
        public float Price { get; set; }

        [JsonProperty("r")]
        public int PriceType { get; set; }

        [JsonProperty("a")]
        public int State { get; set; }

        [JsonProperty("i")]
        public List<string> Photos { get; set; }

        [JsonProperty("b")]
        public string BaseSignature { get; set; }

        [JsonProperty("u")]
        public string BuyerSignature { get; set; }

        [JsonProperty("o")]
        public string BuyerOnionEndpoint { get; set; }

        [JsonProperty("c")]
        public DateTime CreatedUtc { get; set; }

        [JsonProperty("s")]
        public string Signature { get; set; }

        [JsonProperty("h")]
        public string Hash { get; set; }

        /// <summary>
        /// Fineness for a given material, for example "999 fine rhodium", "24 karat gold", "999.9 four nines fine silver"
        /// </summary>
        [JsonProperty("f")]
        public string Fineness { get; set; }

        /// <summary>
        /// To standardise the price per gram and etc, conversion into common metric unit.
        /// </summary>
        [JsonProperty("w")]
        public long WeightInGrams { get; set; }

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

        /* Rendering Helpers */
        [JsonIgnore]
        public virtual List<Bitmap> PrePhotos { get; set; }

        [JsonIgnore]
        public virtual Bitmap PreTitlePhoto { get; set; }

        [JsonIgnore]
        public virtual bool IsInPool { get; set; }

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
            var shaProcessor = new SHAProcessor();

            content.Append(nametype);
            content.Append(Title);
            content.Append(Description);
            content.Append(Shipping);
            content.Append(DealType);
            content.Append(Category);
            content.Append(Price);
            content.Append(PriceType);
            content.Append(State);
            content.Append(BuyerSignature);
            content.Append(BuyerOnionEndpoint);
            content.Append(string.Join(string.Empty, Photos.ToArray()));
            content.Append(BaseSignature);
            content.Append(CreatedUtc.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"));
            content.Append(Fineness);
            if (WeightInGrams>0)
            {
                content.Append(WeightInGrams);
            }            
            content.Append(Size);
            content.Append(Manufacturer);
            return shaProcessor.GetSHA256(content.ToString());
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
            content.Append(Fineness);
            if (WeightInGrams > 0)
            {
                content.Append(WeightInGrams);
            }
            content.Append(Size);
            content.Append(Manufacturer);
            return Encoding.ASCII.GetBytes(content.ToString());
        }
    }
}
