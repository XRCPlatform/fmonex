using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FreeMarketOne.Search
{
    public class SearchEngine
    {
        Directory fSDirectory = null;
        Directory txFSDirectory = null;
        DirectoryReader indexReader = null;
        TaxonomyReader taxoReader = null;
        FacetsConfig facetConfig = null;
        IMarketManager? marketManager = null;
        private readonly int MAX_RESULTS = 1000;

        public int HitsPerPage { get; set; }
        public IndexSearcher Searcher { get; set; }
        public List<string> FacetFieldNames { get; set; }

        public SearchEngine(IMarketManager marketChainManager, string searchIndexBasePath, int hitsPerPage = 20)
        {
            string indexDir = searchIndexBasePath;
            string taxoDir = System.IO.Path.Combine(indexDir,"taxonomy").ToString();

            //validate search
            fSDirectory = FSDirectory.Open(indexDir);
            txFSDirectory = FSDirectory.Open(taxoDir);
            indexReader = DirectoryReader.Open(fSDirectory);
            Searcher = new IndexSearcher(indexReader);
            taxoReader = new DirectoryTaxonomyReader(txFSDirectory);
            facetConfig = new FacetsConfig();
            HitsPerPage = hitsPerPage;
            marketManager = marketChainManager;
            FacetFieldNames = new List<string> { "DealType", "Category", "Shipping", "Fineness", "Manufacturer", "Size", "Sold", "WeightInGrams", "PricePerGram", "Price" };
            //facetConfig.SetHierarchical("Category", true);

        }

        public List<FacetResult> GetFacetsForQuery(Query query)
        {
            return GetFacetsForQuery(query, FacetFieldNames);
        }

        public List<FacetResult> GetFacetsForQuery(Query query, List<string> FacetFieldNames)
        {
            FacetsCollector fc = new FacetsCollector();

            Searcher.Search(query, fc);

            List<FacetResult> results = new List<FacetResult>();

            Facets facets = new FastTaxonomyFacetCounts(taxoReader, facetConfig, fc);
            //Facets facets = new TaxonomyFacetCounts(new DocValuesOrdinalsReader(),taxoReader, facetConfig, fc);

            foreach (var facet in FacetFieldNames)
            {
                var f = facets.GetTopChildren(10, facet);
                if (f != null){
                    results.Add(f);
                }                
            }

            return results;
        }

        public List<FacetResult> GetFacetsForAllDocuments()
        {
            return GetFacetsForQuery(new MatchAllDocsQuery());
        }

        public Query ParseQuery(string phrase, bool includeDims = true)
        {
            LuceneVersion version = LuceneVersion.LUCENE_48;
            StandardAnalyzer analyzer = new StandardAnalyzer(version);

            BooleanQuery bq = new BooleanQuery();
   
            bq.Add(new QueryParser(version, "Title", analyzer).Parse(phrase), Occur.SHOULD);
            bq.Add(new QueryParser(version, "Description", analyzer).Parse(phrase), Occur.SHOULD);

            //facets (removed as facet counts throw indexOutOfRange exception. Looks like the bug in lucene itself. 
            //When fixed, could re-enable or run facet counts on a separate query.
            if (includeDims)
            {
                bq.Add(new QueryParser(version, "Category", analyzer).Parse(phrase), Occur.SHOULD);
                bq.Add(new QueryParser(version, "Manufacturer", analyzer).Parse(phrase), Occur.SHOULD);
            }

            return bq;        

        }

        public DrillDownQuery BuildDrillDown(List<Selector> selectors, Query baseQuery)
        {
            DrillDownQuery drillDownQuery = new DrillDownQuery(facetConfig);
            if (baseQuery != null)
            {
                drillDownQuery = new DrillDownQuery(facetConfig, baseQuery);
            }
            
            foreach (var selector in selectors)
            {
                drillDownQuery.Add(selector.Name, selector.Value);
            }            
            return drillDownQuery;

        }

    
        /// <summary>
        /// This allows performing dimsearch too as long as facet query is not selected, due to bug.
        /// </summary>
        /// <param name="queryPhrase"></param>
        /// <param name="queryFacets"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public SearchResult Search(string queryPhrase, bool queryFacets = true, int page = 1)
        {
            var query = ParseQuery(queryPhrase, !queryFacets);
            return Search(query, queryFacets, page);
        }

        public SearchResult Search(Query query, bool queryFacets = true, int page = 1)
        {
            List<MarketItem> list = new List<MarketItem>();
            TopScoreDocCollector collector = TopScoreDocCollector.Create(MAX_RESULTS, true);
            int startIndex = (page - 1) * HitsPerPage;
            Searcher.Search(query, collector);
            TopDocs docs = collector.GetTopDocs(startIndex, HitsPerPage);
            ScoreDoc[] hits = docs.ScoreDocs;

            foreach (var item in hits)
            {
                var marketItem = JsonConvert.DeserializeObject<MarketItem>(Searcher.Doc(item.Doc).Get("MarketItem"));
                if (marketItem != null)
                {
                    list.Add(marketItem);
                }
                //var offer = marketManager.GetOfferBySignature(signature);
            }
            List<FacetResult> facets = new List<FacetResult>();
            if (queryFacets)
            {
                facets = GetFacetsForQuery(query);
            }
            SearchResult searchResult = new SearchResult
            {
                Results = list,
                Facets = facets,
                TotalHits = docs.TotalHits,
                CurrentPage = page,
                PageSize = HitsPerPage,
                CurrentQuery = query
            };
            return searchResult;
        }

        /// <summary>
        /// One of the list of pubkeys will be indexed. We should seek with all to find a right one.
        /// </summary>
        /// <param name="sellerPubKeys"></param>
        /// <returns></returns>
        public Query BuildQueryBySellerPubKeys(List<byte[]> sellerPubKeys) {

            BooleanQuery bq = new BooleanQuery();
            var sellerPubKeyHashes = SearchHelper.GenerateSellerPubKeyHashes(sellerPubKeys);
            foreach (var sellerPubKeyHash in sellerPubKeyHashes)
            {
                bq.Add(new TermQuery(new Term("SellerPubKeyHash", sellerPubKeyHash)), Occur.SHOULD);
            }
            return bq;
        }


        /// <summary>
        /// Builds query to retrieve market item by Seller Signature (ID)
        /// </summary>
        /// <param name="signature"></param>
        /// <returns></returns>
        public Query BuildQueryBySignature(string signature)
        {
            return new TermQuery(new Term("ID", signature));
        }


        //TODO: Relevance ranks biased by Seller Reputation scores. Higher scored stars, more successful high value deals closed, staking deposits and etc could comprise seller reputation.
    }
}
