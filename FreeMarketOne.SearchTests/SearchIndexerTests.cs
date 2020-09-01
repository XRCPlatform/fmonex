using Microsoft.VisualStudio.TestTools.UnitTesting;
using FreeMarketOne.Search;
using System;
using System.Collections.Generic;
using System.Text;
using FreeMarketOne.DataStructure.Objects.MarketItems;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet;
using Lucene.Net.Store;
using Lucene.Net.Facet.Taxonomy.Directory;

namespace FreeMarketOne.Search.Tests
{
    [TestClass()]
    public class SearchIndexerTests
    {
    
        [TestMethod()]
        public void CorrectlyMappsCategoryAndDealTypeFacets()
        {
            MarketItem marketItem = new MarketItem
            {
                Hash = "A",
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

            SearchIndexer search = new SearchIndexer(indexDir);
            search.Index(marketItem);

            //validate search
            Directory fSDirectory = new RAMDirectory();
            Directory txFSDirectory = new RAMDirectory();//FSDirectory.Open(taxoDir);
            DirectoryReader indexReader = DirectoryReader.Open(fSDirectory);
            IndexSearcher searcher = new IndexSearcher(indexReader);
            TaxonomyReader taxoReader = new DirectoryTaxonomyReader(txFSDirectory);
            FacetsConfig facetConfig = new FacetsConfig();
            FacetsCollector fc = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query:
            searcher.Search(new MatchAllDocsQuery(), null /*Filter */, fc);

            // Retrieve results
            List<FacetResult> results = new List<FacetResult>();

            // Count both "Publish Date" and "Author" dimensions
            Facets facets = new FastTaxonomyFacetCounts(taxoReader, facetConfig, fc);

            results.Add(facets.GetTopChildren(10, "DealType"));
            results.Add(facets.GetTopChildren(10, "Category"));


            indexReader.Dispose();
            taxoReader.Dispose();
            Assert.Fail();
        }
    }
}