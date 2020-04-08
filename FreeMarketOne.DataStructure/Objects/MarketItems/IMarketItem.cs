using JsonSubTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.MarketItems
{
    [JsonConverter(typeof(JsonSubtypes), "_nt")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(MarketItemV1), "MarketItemV1")]
    public interface IMarketItem
    {
        string nametype { get; set; }
        int Title { get; set; }
        string Hash { get; set; }
        string Description { get; set; }
        DateTime CreatedUtc { get; set; }
        bool IsValid();
        string GenerateHash();
    }
}
