using FreeMarketOne.DataStructure.Objects.BaseItems;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Chat
{
    public class ChatData : IChatData
    {
        [JsonProperty("_nt")]
        public string nametype { get; set; }

        [JsonProperty("m")]
        public MarketItemV1 MarketItem { get; set; }

        [JsonProperty("c")]
        public List<ChatItem> ChatItems { get; set; }

        [JsonProperty("s")]
        public string SellerEndPoint { get; set; }

        [JsonProperty("d")]
        public DateTime DateCreated { get; set; }
    }
}
