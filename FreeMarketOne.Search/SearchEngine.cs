using FreeMarketOne.DataStructure.Objects.BaseItems;
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
using FreeMarketOne.Markets;
using Lucene.Net.Documents;
using System;

namespace FreeMarketOne.Search
{
    public class SearchEngine
    {
        Directory fSDirectory = null;
        Directory txFSDirectory = null;
        FacetsConfig facetConfig = null;
        IMarketManager? marketManager = null;
        private readonly int MAX_RESULTS = 1000;
        private NormalizedStore _normalizedStore = null;
        private DirectoryReader _indexReader;
        public int PageSize { get; set; }
        public List<string> FacetFieldNames { get; set; }

        public SearchEngine(IMarketManager marketChainManager, string searchIndexBasePath, int hitsPerPage = 20)
        {
            string indexDir = searchIndexBasePath;
            string taxoDir = System.IO.Path.Combine(indexDir, "taxonomy").ToString();

            //validate search
            fSDirectory = FSDirectory.Open(indexDir);
            txFSDirectory = FSDirectory.Open(taxoDir);
            facetConfig = new FacetsConfig();
            PageSize = hitsPerPage;
            marketManager = marketChainManager;
            FacetFieldNames = new List<string> { "Category", "Shipping", "Fineness", "Manufacturer", "Size", "WeightInGrams", "PricePerGram", "Price" };
            //facetConfig.SetHierarchical("Category", true);
            _normalizedStore = new NormalizedStore(searchIndexBasePath);

            //this could be buggy with index reading errors if it happens use local variable within each query
            // it is very costly 3.5% of query time to open this so moved here to reuse
            // however this is not ready if indexer is not primed.
            //_indexReader = DirectoryReader.Open(fSDirectory);

        }

        public List<FacetResult> GetFacetsForQuery(Query query)
        {
            try
            {
                return GetFacetsForQuery(query, FacetFieldNames);
            }
            catch (Exception)
            {
                try
                {
                    //recurse but if we lost facets go without. Only relevant in IBD hight velocity writes.
                    return GetFacetsForQuery(query);
                }
                catch (Exception )
                {
                    return null;
                }
            }
        }

        public List<FacetResult> GetFacetsForQuery(Query query, List<string> FacetFieldNames)
        {
            FacetsCollector fc = new FacetsCollector();

            var indexReader = DirectoryReader.Open(fSDirectory);
            var searcher = new IndexSearcher(indexReader);
            searcher.Search(query, fc);

            List<FacetResult> results = new List<FacetResult>();
            var taxoReader = new DirectoryTaxonomyReader(txFSDirectory);

            Facets facets = new FastTaxonomyFacetCounts(taxoReader, facetConfig, fc);

            foreach (var facet in FacetFieldNames)
            {
                var f = facets.GetTopChildren(10, facet);
                if (f != null)
                {
                    results.Add(f);
                }
            }

            return results;
        }

        public List<FacetResult> GetFacetsForAllDocuments()
        {
            return GetFacetsForQuery(new MatchAllDocsQuery());
        }

        public bool ValidateQuery(string phrase)
        {
            try
            {
                ParseQuery(phrase);
                return true;
            }
            catch (System.Exception)
            {

                return false;
            }
        }

        public Query ParseQuery(string phrase)
        {
            if (string.IsNullOrEmpty(phrase) || string.IsNullOrWhiteSpace(phrase))
            {
                return new MatchAllDocsQuery();
            }
            LuceneVersion version = LuceneVersion.LUCENE_48;
            StandardAnalyzer analyzer = new StandardAnalyzer(version);

            BooleanQuery bq = new BooleanQuery();

            bq.Add(new QueryParser(version, "Title", analyzer).Parse(phrase), Occur.SHOULD);
            bq.Add(new QueryParser(version, "Description", analyzer).Parse(phrase), Occur.SHOULD);

            bq.Add(new QueryParser(version, "Category", analyzer).Parse(phrase), Occur.SHOULD);
            bq.Add(new QueryParser(version, "Manufacturer", analyzer).Parse(phrase), Occur.SHOULD);

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
        /// This allows performing dimsearch too.
        /// </summary>
        /// <param name="queryPhrase"></param>
        /// <param name="queryFacets"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public SearchResult Search(string queryPhrase, bool queryFacets = true, int page = 1)
        {
            Query query = new MatchAllDocsQuery();
            if (!string.IsNullOrEmpty(queryPhrase) || !string.IsNullOrWhiteSpace(queryPhrase))
            {
                query = ParseQuery(queryPhrase);
            }
            return Search(query, queryFacets, page);
        }

        public SearchResult Search(Query query, bool queryFacets = true, int page = 1)
        {
            List<MarketItemV1> list = new List<MarketItemV1>();
            List<Document> documents = new List<Document>();
            TopScoreDocCollector collector = TopScoreDocCollector.Create(MAX_RESULTS, true);
            int startIndex = (page - 1) * PageSize;

            var indexReader = DirectoryReader.Open(fSDirectory);
            var searcher = new IndexSearcher(indexReader);
            TopDocs docs;
            ScoreDoc[] hits;
            try
            {

                searcher.Search(query, collector);
                docs = collector.GetTopDocs(startIndex, PageSize);
                hits = docs.ScoreDocs;
            }
            catch (Exception)
            {
                //rerun in case of indexing concurency exception
                //important to allow intensive indexing of IBD and give better ux
                searcher.Search(query, collector);
                docs = collector.GetTopDocs(startIndex, PageSize);
                hits = docs.ScoreDocs;
            }


            foreach (var item in hits)
            {
                var marketItem = JsonConvert.DeserializeObject<MarketItemV1>(searcher.Doc(item.Doc).Get("MarketItem"));
                if (marketItem != null)
                {
                    list.Add(marketItem);
                }

                //adding raw document so that relevance could be better understood
                //documents.Add(searcher.Doc(item.Doc));
            }
            List<FacetResult> facets = new List<FacetResult>();
            if (queryFacets)
            {
                try
                {
                    facets = GetFacetsForQuery(query);
                }
                catch (Exception)
                {
                    //retry during agressive optimistic indexing sometimes we fail here as indexing is not fully atomic
                    facets = GetFacetsForQuery(query);
                }

            }
            SearchResult searchResult = new SearchResult
            {
                Results = list,
                Facets = facets,
                TotalHits = docs.TotalHits,
                CurrentPage = page,
                PageSize = PageSize,
                CurrentQuery = query,
                Documents = documents
            };
            return searchResult;
        }

        /// <summary>
        /// One of the list of pubkeys will be indexed. We should seek with all to find a right one.
        /// </summary>
        /// <param name="sellerPubKeys"></param>
        /// <returns></returns>
        public Query BuildQueryBySellerPubKeys(List<byte[]> sellerPubKeys)
        {

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

        /// <summary>
        /// GetMyOffers retruns My Offers Sold, or Bought. Open offers are returned as part of normal search.
        /// </summary>
        /// <param name="offerDirection"></param>
        /// <param name="pageSize"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public SearchResult GetMyCompletedOffers(OfferDirection offerDirection, int pageSize, int page)
        {
            return _normalizedStore.GetMyOffers(offerDirection, pageSize, page);
        }

        /// <summary>
        /// Returns user object by pubkey;
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public UserDataV1 GetUser(string pubKey)
        {
            return _normalizedStore.GetUser(pubKey);
        }

        public UserDataV1 GetUser(byte[] pubKey)
        {
            var publicKeyString = Convert.ToBase64String(pubKey);
            return GetUser(publicKeyString);
        }

        public UserDataV1 GetUser(List<byte[]> pubKeys)
        {
            foreach (var pubKey in pubKeys)
            {
                var publicKeyString = Convert.ToBase64String(pubKey);
                var user = GetUser(publicKeyString);
                if (user != null)
                {
                    return user;
                }
            }
            return null;
        }

        public List<ReviewUserDataV1>? GetAllReviewsForPubKey(List<byte[]> pubKeys)
        {
            List<ReviewUserDataV1>? list = new List<ReviewUserDataV1>();
            foreach (var pubKey in pubKeys)
            {
                var publicKeyString = Convert.ToBase64String(pubKey);
                list = GetAllReviewsForPubKey(publicKeyString);
                if (list != null && list.Count > 0)
                {
                    return list;
                }
            }
            return list;
        }

        public List<ReviewUserDataV1> GetAllReviewsForPubKey(byte[] pubKey)
        {
            var publicKeyString = Convert.ToBase64String(pubKey);
            return GetAllReviewsForPubKey(publicKeyString);
        }

        public List<ReviewUserDataV1> GetAllReviewsForPubKey(string pubKey)
        {
            return _normalizedStore.GetAllReviewsByPubKey(pubKey);
        }


        //TODO: Relevance ranks biased by Seller Reputation scores. Higher scored stars, more successful high value deals closed, staking deposits and etc could comprise seller reputation.
    }
}
