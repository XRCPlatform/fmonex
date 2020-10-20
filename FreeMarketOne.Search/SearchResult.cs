using FreeMarketOne.DataStructure.Objects.BaseItems;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;

namespace FreeMarketOne.Search
{
    public class SearchResult
    {
        int totalHits;
        public List<MarketItemV1> Results { get; set; } = new List<MarketItemV1>();
        public List<FacetResult> Facets { get; set; } = new List<FacetResult>();
        public int TotalHits { get => totalHits; set => totalHits = value; }
        public int CurrentPage { get; internal set; }
        public Query CurrentQuery { get; internal set; }
        public int PageSize { get; internal set; }

        public List<Document> Documents { get; set; } = new List<Document>();

        //currently applied facet filters
        //add isMyProduct flag?
    }
}