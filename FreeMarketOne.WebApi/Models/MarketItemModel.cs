using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Newtonsoft.Json;

namespace FreeMarketOne.WebApi.Models
{
    public class MarketItemModel
    {
        [JsonProperty("nameType")]
        public string NameType { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("shipping")]
        public string Shipping { get; set; }

        [JsonProperty("dealType")]
        public int DealType { get; set; }

        [JsonProperty("category")]
        public int Category { get; set; }

        [JsonProperty("price")]
        public float Price { get; set; }

        [JsonProperty("priceType")]
        public int PriceType { get; set; }

        [JsonProperty("state")]
        public int State { get; set; }

        [JsonProperty("photoUrls")]
        public List<string> Photos { get; set; }

        [JsonProperty("baseSignature")]
        public string BaseSignature { get; set; }

        [JsonProperty("buyerSignature")]
        public string BuyerSignature { get; set; }

        [JsonProperty("buyerOnionEndpoint")]
        public string BuyerOnionEndpoint { get; set; }

        [JsonProperty("createdUtc")]
        public DateTime CreatedUtc { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("hash")]
        public string Hash { get; set; }

        [JsonProperty("fineness")]
        public string Fineness { get; set; }

        [JsonProperty("weightInGrams")]
        public long WeightInGrams { get; set; }

        [JsonProperty("size")]
        public string Size { get; set; }

        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }
        
        [JsonProperty("xrcReceivingAddress")]
        public string XrcReceivingAddress { get; set; }
        
        [JsonProperty("xrcTransctionHash")]
        public string XrcTransactionHash { get; set; }

        public static MarketItemModel FromMarketItem(MarketItem marketItem)
        {
            return new()
            {
                NameType = marketItem.nametype,
                Title = marketItem.Title,
                Description = marketItem.Description,
                Shipping = marketItem.Shipping,
                DealType = marketItem.DealType,
                Category = marketItem.Category,
                Price = marketItem.Price,
                PriceType = marketItem.PriceType,
                State = marketItem.State,
                Photos = marketItem.Photos,
                BaseSignature = marketItem.BaseSignature,
                BuyerSignature = marketItem.BuyerSignature,
                BuyerOnionEndpoint = marketItem.BuyerOnionEndpoint,
                CreatedUtc = marketItem.CreatedUtc,
                Signature = marketItem.Signature,
                Hash = marketItem.Hash,
                Fineness = marketItem.Fineness,
                WeightInGrams = marketItem.WeightInGrams,
                Size = marketItem.Size,
                Manufacturer = marketItem.Manufacturer,
                XrcReceivingAddress = marketItem.XRCReceivingAddress,
                XrcTransactionHash = marketItem.XRCTransactionHash
            };
        }
        
    }
}