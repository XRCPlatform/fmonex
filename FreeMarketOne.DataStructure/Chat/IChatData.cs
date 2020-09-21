using FreeMarketOne.DataStructure.Objects.BaseItems;
using JsonSubTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FreeMarketOne.DataStructure.Chat
{
    [JsonConverter(typeof(JsonSubtypes), "_nt")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(ChatDataV1), "ChatDataV1")]
    public interface IChatData
    {
        string nametype { get; set; }
        MarketItemV1 MarketItem { get; set; }
        List<ChatItem> ChatItems { get; set; }
        string SellerEndPoint { get; set; }
        DateTime DateCreated { get; set; }
    }
}
