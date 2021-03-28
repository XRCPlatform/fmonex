using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.Net.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FreeMarketOne.DataStructure.Chat
{
    public class ChatData : IChatData
    {
        [JsonProperty("_nt")]
        public string nametype { get; set; }

        /// <summary>
        /// This is optional and will mostly exist but chat lifetime is not aligned with market item lifetime so eventually market item will be pruned
        /// </summary>
        [JsonProperty("m")]
        public MarketItemV1 MarketItem { get; set; }

        /// <summary>
        /// The market item is a temporary creature, it will eventually cease to exist
        /// Market item hash is like topic for the conversation. If instance of item exists great, but if not, topic should still exist
        /// </summary>
        [JsonProperty("h")]
        public string MarketItemHash { get; set; }

        [JsonProperty("c")]
        public List<ChatItem> ChatItems { get; set; }

        [JsonProperty("s")]
        public string SellerEndPoint { get; set; }

        [JsonProperty("d")]
        public DateTime DateCreated { get; set; }
    }
}
