using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Search;
using Lucene.Net.Facet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace FreeMarketApp.ViewModels
{
    public class MyProductsPageViewModel : ViewModelBase
    {
        public MyProductsPageViewModel(SearchResult searchResult, IEnumerable<MarketItemV1> soldItems, List<Selector> appliedFilters)
        {
            AppliedFilters = appliedFilters;
            Items = new ObservableCollection<MarketItemV1>((IEnumerable<MarketItemV1>)searchResult.Results);
            SoldItems = new ObservableCollection<MarketItemV1>(soldItems);
            Facets = new ObservableCollection<FacetResult>((IEnumerable<FacetResult>)searchResult.Facets);
            Filters = new ObservableCollection<PresentableFacet>((IEnumerable<PresentableFacet>)JoinAppliedFilters(AppliedFilters, searchResult.Facets));
            Result = searchResult;
        }
     
        public List<Selector> AppliedFilters { get; private set; }
        public ObservableCollection<MarketItemV1> Items { get; set; }
        public ObservableCollection<FacetResult> Facets { get; }
        public ObservableCollection<PresentableFacet> Filters { get; private set; }
        public ObservableCollection<MarketItemV1> SoldItems { get; set; }

        /// <summary>
        /// Search result sumary object providing search result counts, page size, current page and etc.
        /// </summary>
        public SearchResult Result { get; set; }

        /// <summary>
        /// Helper property to show/hide next page button.
        /// </summary>
        public bool ShowNextPage
        {
            get
            {
                return Result.TotalHits > (Result.PageSize * Result.CurrentPage);
            }
        }

        /// <summary>
        /// Helper property to show/hide next page button.
        /// </summary>
        public bool ShowPreviousPage
        {
            get
            {
                return Result.CurrentPage > 1;
            }
        }

        private List<PresentableFacet> JoinAppliedFilters(List<Selector> selectors, List<FacetResult> facetResults)
        {
            var result = new List<PresentableFacet>();
            foreach (var facetResult in facetResults)
            {
                var selected = selectors.Find(selector => selector.Name == facetResult.Dim);
                if (selected != null)
                {
                    result.Add(new PresentableFacet(facetResult, true, true));
                }
                else
                {
                    result.Add(new PresentableFacet(facetResult, false, false));
                }

            }
            return result;
        }
    }
}
