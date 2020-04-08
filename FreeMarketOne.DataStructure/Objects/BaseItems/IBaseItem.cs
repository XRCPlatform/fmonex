using JsonSubTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Objects.BaseItems
{
    [JsonConverter(typeof(JsonSubtypes), "_nt")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(CheckPointMarketDataV1), "CheckPointMarketDataV1")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(ReviewUserDataV1), "ReviewUserDataV1")]
    public interface IBaseItem
    {
        string nametype { get; set; }
        string Hash { get; set; }
        DateTime CreatedUtc { get; set; }
        bool IsValid();
    }
}
