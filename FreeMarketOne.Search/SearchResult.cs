using FreeMarketOne.DataStructure.Objects.BaseItems;
using Lucene.Net.Facet;
using System;
using System.Collections.Generic;

namespace FreeMarketOne.Search
{
    public class SearchResult
    {
        public List<MarketItem> Results { get; set; } = new List<MarketItem>();
        public List<FacetResult> Facets { get; set; } = new List<FacetResult>();
    }
}