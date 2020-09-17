using FreeMarketOne.DataStructure.Objects.BaseItems;
using System;
using System.Collections.Generic;

namespace FreeMarketOne.Search
{
    public class SearchResult
    {
        public List<MarketItem> Results { get; set; } = new List<MarketItem>();
    }
}