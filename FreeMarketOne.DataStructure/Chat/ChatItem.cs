﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Chat
{
    public class ChatItem
    {
        [JsonProperty("m")]
        public string Message { get; set; }

        [JsonProperty("x")]
        public string ExtraMessage { get; set; }

        [JsonProperty("d")]
        public DateTime DateCreated { get; set; }

        [JsonProperty("t")]
        public int Type { get; set; }

        [JsonProperty("p")]
        public bool Propagated { get; set; }
    }
}
