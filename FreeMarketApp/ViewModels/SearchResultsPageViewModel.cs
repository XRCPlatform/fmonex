﻿using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Search;
using Lucene.Net.Facet;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FreeMarketApp.ViewModels
{
    public class SearchResultsPageViewModel: ViewModelBase
    {
        public SearchResultsPageViewModel(SearchResult searchResult)
        {
            Items = new ObservableCollection<MarketItem>((IEnumerable<MarketItem>)searchResult.Results);
            Facets = new ObservableCollection<FacetResult>((IEnumerable<FacetResult>)searchResult.Facets);
            Result = searchResult;
        }
        public ObservableCollection<MarketItem> Items { get; set; }
        public ObservableCollection<FacetResult> Facets { get; set; }
        public SearchResult Result { get; set; }
    }
}
