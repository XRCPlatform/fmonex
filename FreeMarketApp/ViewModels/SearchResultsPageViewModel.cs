using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Search;
using Lucene.Net.Facet;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FreeMarketApp.ViewModels
{
    public class SearchResultsPageViewModel : ViewModelBase
    {
        public SearchResultsPageViewModel(SearchResult searchResult)
        {
            Items = new ObservableCollection<MarketItemV1>((IEnumerable<MarketItemV1>)searchResult.Results);
            Facets = new ObservableCollection<FacetResult>((IEnumerable<FacetResult>)searchResult.Facets);
            Result = searchResult;
        }
        public ObservableCollection<MarketItemV1> Items { get; set; }
        public ObservableCollection<FacetResult> Facets { get; set; }
        public SearchResult Result { get; set; }

        public bool ShowNextPage
        {
            get
            {
                return Result.TotalHits > (Result.PageSize * Result.CurrentPage);
            }
        }
        public bool ShowPreviousPage
        {
            get
            {
                return Result.CurrentPage > 1;
            }
        }
    }
}
