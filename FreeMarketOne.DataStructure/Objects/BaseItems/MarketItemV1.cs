using System;
using System.Text.Json.Serialization;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    public class MarketItemV1 : MarketItem
    {
        public MarketItemV1()
        {
            this.nametype = "MarketItemV1";
        }

        [JsonIgnore]
        public bool Reviewed { get; set; }
    }
}
