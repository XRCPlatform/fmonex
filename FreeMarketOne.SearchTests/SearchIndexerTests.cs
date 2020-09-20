using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace FreeMarketOne.Search.Tests
{
    [TestClass()]
    public class SearchIndexerTests
    {
    
        [TestMethod()]
        public void CorrectlyCountsFacetsOnTermQuery()
        {
            MarketItemV1 marketItem = new MarketItemV1
            {
                Signature = "A",
                CreatedUtc = DateTime.UtcNow,
                DealType = 3,
                Category = 2,
                Price = 10868.4F,
                BuyerSignature = "a",
                Description = "The Baird & Co. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                Title = "1 Kilogram Gold Cast Bar Baird & Co",
                Shipping = "International"
            };
            
            string indexDir = "./search";
            string taxoDir = indexDir + "/taxonomy/";

            var marketManager = Substitute.For<IMarketManager>();
            SearchIndexer search = new SearchIndexer(indexDir, marketManager);
            search.DeleteAll();
            search.Commit();

            search.Index(marketItem,"block-hash");
            search.Commit();

            //validate search
            Directory fSDirectory = FSDirectory.Open(indexDir);
            Directory txFSDirectory = FSDirectory.Open(taxoDir);
            DirectoryReader indexReader = DirectoryReader.Open(fSDirectory);
            IndexSearcher searcher = new IndexSearcher(indexReader);
            TaxonomyReader taxoReader = new DirectoryTaxonomyReader(txFSDirectory);
            FacetsConfig facetConfig = new FacetsConfig();
            FacetsCollector fc = new FacetsCollector();

            searcher.Search(new TermQuery(new Term("ID", "A")), null, fc);
            //TopDocs topDocs = searcher.Search(new TermQuery(new Term("ID","Z")),1);
            //var doc = searcher.Document(topDocs.ScoreDocs[0].Doc, null);
            // Retrieve results
            List<FacetResult> results = new List<FacetResult>();

            Facets facets = new FastTaxonomyFacetCounts(taxoReader, facetConfig, fc);

            results.Add(facets.GetTopChildren(10, "DealType"));
            results.Add(facets.GetTopChildren(10, "Category"));
            Assert.AreEqual(results.Count, 2);
            Assert.AreEqual(results[0].Dim, "DealType");

            //search for doc that won't exist
            FacetsCollector fc2 = new FacetsCollector();
            searcher.Search(new TermQuery(new Term("ID", Guid.NewGuid().ToString())), null, fc2);
            Facets facets2 = new FastTaxonomyFacetCounts(taxoReader, facetConfig, fc2);
            Assert.AreEqual(facets2.GetAllDims(10).Count,0);
            Assert.IsNull(facets2.GetTopChildren(10, "Category"));

            indexReader.Dispose();
            taxoReader.Dispose();
            search.DeleteAll();
            search.Dispose();

        }


        [TestMethod()]
        public void CorrectlyCountsCategoryAndDealTypeFacetsWithNoFilters()
        {
            MarketItemV1 marketItem = new MarketItemV1
            {
                Signature = "A",
                CreatedUtc = DateTime.UtcNow,
                DealType = 1,
                Category = 2,
                Price = 10868.4F,
                BuyerSignature = "a",
                Description = "The Baird & Co. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                Title = "1 Kilogram Gold Cast Bar Baird & Co",
                Shipping = "International"
            };

            MarketItemV1 marketItem2 = new MarketItemV1
            {
                Signature = "B",
                CreatedUtc = DateTime.UtcNow,
                DealType = 3,
                Category = 1,
                Price = 10868.4F,
                BuyerSignature = "a",
                Description = "The Baird & Co. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                Title = "1 Kilogram Gold Cast Bar Baird & Co",
                Shipping = "International"
            };

            MarketItemV1 marketItem3 = new MarketItemV1
            {
                Signature = "C",
                CreatedUtc = DateTime.UtcNow,
                DealType = 3,
                Category = 1,
                Price = 10868.4F,
                BuyerSignature = "a",
                Description = "The Baird & Co. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                Title = "1 Kilogram Gold Cast Bar Baird & Co",
                Shipping = "Europe"
            };

            string indexDir = "./search";
            string taxoDir = indexDir + "/taxonomy/";

            var marketManager = Substitute.For<IMarketManager>();
            SearchIndexer search = new SearchIndexer(indexDir, marketManager);
            search.DeleteAll();
            search.Commit();
            search.Index(marketItem, "block-hash");
            search.Index(marketItem2, "block-hash");
            search.Index(marketItem3, "block-hash");
            search.Commit();

            //validate search
            Directory fSDirectory = FSDirectory.Open(indexDir);
            Directory txFSDirectory = FSDirectory.Open(taxoDir);
            DirectoryReader indexReader = DirectoryReader.Open(fSDirectory);
            IndexSearcher searcher = new IndexSearcher(indexReader);
            TaxonomyReader taxoReader = new DirectoryTaxonomyReader(txFSDirectory);
            FacetsConfig facetConfig = new FacetsConfig();
            FacetsCollector fc = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query:
            searcher.Search(new MatchAllDocsQuery(), null /*Filter */, fc);

            List<FacetResult> results = new List<FacetResult>();

            Facets facets = new FastTaxonomyFacetCounts(taxoReader, facetConfig, fc);

            results.Add(facets.GetTopChildren(10, "DealType"));
            results.Add(facets.GetTopChildren(10, "Category"));
            results.Add(facets.GetTopChildren(10, "Shipping"));


            indexReader.Dispose();
            taxoReader.Dispose();
            Assert.AreEqual(results.Find(x => x.Dim.Equals("Category")).ChildCount,2);
            Assert.AreEqual(results.Find(x => x.Dim.Equals("Category")).LabelValues[0].Label, "Gold");
            Assert.AreEqual(results.Find(x => x.Dim.Equals("Category")).LabelValues[0].Value, 2);
            Assert.AreEqual(results.Find(x => x.Dim.Equals("Category")).LabelValues[1].Label, "Silver");
            Assert.AreEqual(results.Find(x => x.Dim.Equals("Category")).LabelValues[1].Value, 1);

            Assert.AreEqual(results.Find(x => x.Dim.Equals("Shipping")).ChildCount, 2);
            Assert.AreEqual(results.Find(x => x.Dim.Equals("Shipping")).LabelValues[0].Label, "International");
            Assert.AreEqual(results.Find(x => x.Dim.Equals("Shipping")).LabelValues[0].Value, 2);
            Assert.AreEqual(results.Find(x => x.Dim.Equals("Shipping")).LabelValues[1].Label, "Europe");
            Assert.AreEqual(results.Find(x => x.Dim.Equals("Shipping")).LabelValues[1].Value, 1);
            search.DeleteAll();
            search.Commit();
            search.Dispose();
        }


        [TestMethod()]
        public void FindsRhodiumBarBySearchPhrase()
        {

            MarketItemV1 marketItem = new MarketItemV1
            {
                Signature = "F",
                CreatedUtc = DateTime.UtcNow,
                DealType = 1,
                Category = 4,
                Price = 10868.4F,
                BuyerSignature = "a",
                Description = "These one ounce minted rhodium bars are produced by Baird & Co in London, England.Each bar is individually numbered, supplied as new in mint packaging and contains 31.1035 grams of 999.0 fine rhodium.",
                Title = "1oz Baird & Co Minted Rhodium Bar",
                Shipping = "International",
                Fineness = "999 fine rhodium",
                Size = "1oz",
                WeightInGrams = 28,
                Manufacturer = "Baird & Co",
            };

            MarketItemV1 marketItem2 = new MarketItemV1
            {
                Signature = "B",
                CreatedUtc = DateTime.UtcNow,
                DealType = 3,
                Category = 1,
                Price = 10868.4F,
                BuyerSignature = "a",
                Description = "The Baird & Co. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                Title = "1 Kilogram Gold Cast Bar Baird & Co",
                Shipping = "International"
            };

            MarketItemV1 marketItem3 = new MarketItemV1
            {
                Signature = "C",
                CreatedUtc = DateTime.UtcNow,
                DealType = 3,
                Category = 1,
                Price = 10868.4F,
                BuyerSignature = "a",
                Description = "The Baird & Co. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                Title = "1 Kilogram Gold Cast Bar Baird & Co",
                Shipping = "Europe"
            };

            string indexDir = "./search";

            var marketManager = Substitute.For<IMarketManager>();

            SearchIndexer search = new SearchIndexer(indexDir, marketManager);
            search.DeleteAll();
            search.Commit();
            search.Index(marketItem, "block-hash");
            search.Index(marketItem2, "block-hash");
            search.Index(marketItem3, "block-hash");
            search.Commit();

            //validate search
            Directory fSDirectory = FSDirectory.Open(indexDir);
            DirectoryReader indexReader = DirectoryReader.Open(fSDirectory);
            IndexSearcher searcher = new IndexSearcher(indexReader);

            LuceneVersion version = LuceneVersion.LUCENE_48;
            StandardAnalyzer analyzer = new StandardAnalyzer(version);


            //Query q = new PhraseQuery(new Term("title", "1oz Baird & Co Minted Rhodium Bar"));
            BooleanQuery bq = new BooleanQuery();
            QueryParser qp = new QueryParser(version,"Title", analyzer);
            Query query = qp.Parse("rhodium bar");
            bq.Add(query, Occur.MUST);
            QueryParser qp1 = new QueryParser(version, "Description", analyzer);
            Query query1 = qp1.Parse("rhodium bar");
            bq.Add(query1, Occur.SHOULD);

            int hitsPerPage = 10;
    
            TopDocs docs = searcher.Search(query, hitsPerPage);
            ScoreDoc[] hits = docs.ScoreDocs;

          
            int docId = hits[0].Doc;
            Document d = searcher.Doc(docId);
            Assert.AreEqual(d.Get("ID"), "F");
            Assert.AreEqual(3, hits.Length);

            indexReader.Dispose();
            search.Dispose();

        }
    }
}