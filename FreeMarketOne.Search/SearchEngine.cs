using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
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

        public int HitsPerPage { get; set; }
        public IndexSearcher Searcher { get; set; }
        public List<string> FacetFieldNames { get; set; }

        public SearchEngine(IMarketManager marketChainManager, string searchIndexBasePath, int hitsPerPage = 20)
        {
            string indexDir = searchIndexBasePath;
            string taxoDir = indexDir + "/taxonomy/";

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
        }

        public List<FacetResult> GetFacetsForQuery(Query query, Filter filter)
        {
            return GetFacetsForQuery(query, filter, FacetFieldNames);
        }

        public List<FacetResult> GetFacetsForQuery(Query query, Filter filter, List<string> FacetFieldNames)
        {
            FacetsCollector fc = new FacetsCollector();
            Searcher.Search(query, filter, fc);

            List<FacetResult> results = new List<FacetResult>();

            Facets facets = new FastTaxonomyFacetCounts(taxoReader, facetConfig, fc);
            
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
            return GetFacetsForQuery(new MatchAllDocsQuery(), null);
        }

        public Query ParseQuery(string phrase)
        {
            LuceneVersion version = LuceneVersion.LUCENE_48;
            StandardAnalyzer analyzer = new StandardAnalyzer(version);

            BooleanQuery bq = new BooleanQuery();
            QueryParser qp = new QueryParser(version, "Title", analyzer);
            Query query = qp.Parse(phrase);
            bq.Add(query, Occur.MUST);
            QueryParser qp1 = new QueryParser(version, "Description", analyzer);
            Query query1 = qp1.Parse(phrase);
            bq.Add(query1, Occur.SHOULD);
            return bq;        

        }

        public SearchResult Search(Query query)
        {
            List<MarketItem> list = new List<MarketItem>();           
            
            TopDocs docs = Searcher.Search(query, HitsPerPage);
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
            SearchResult searchResult = new SearchResult
            {
                Results = list
            };
            return searchResult;
        }
    }
}
