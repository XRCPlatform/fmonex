using Lucene.Net.Facet;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Search
{
    //
    // Summary:
    //     Counts or aggregates for a single dimension.
    public class PresentableFacet
    {
        /// <summary>
        /// Wraps Lucene FacetResult with useful properties that are helping UI rendition.
        /// </summary>
        /// <param name="facetResult"></param>
        /// <param name="IsSelected"></param>
        /// <param name="IsExpanded"></param>
        public PresentableFacet(FacetResult facetResult, bool IsSelected, bool IsExpanded)
        {
            FacetResult = facetResult;
            this.IsSelected = IsSelected;
            this.IsExpanded = IsExpanded;
        }
       
        public FacetResult FacetResult { get; }
        public bool IsSelected { get; }
        public bool IsExpanded { get; }
    }
}
