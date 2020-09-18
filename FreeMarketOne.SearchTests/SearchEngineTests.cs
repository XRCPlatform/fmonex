using FreeMarketOne.BlockChain;
using FreeMarketOne.DataStructure;
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
    public class SearchEngineTests
    {
    
        [TestMethod()]
        public void SearchEngine_CorrectlyCountsFacetsOnTermQuery()
        {
            MarketItem marketItem = new MarketItem
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
         
            var marketManager = Substitute.For<IMarketManager>();
            SearchIndexer search = new SearchIndexer(indexDir, marketManager);
            search.DeleteAll();
            search.Commit();
            search.Index(marketItem,"block-hash");
            search.Commit();

 

            SearchEngine engine = new SearchEngine(marketManager, indexDir);         
            List<FacetResult> results = engine.GetFacetsForQuery(new TermQuery(new Term("ID", "A")), null);

            Assert.AreEqual(7, results.Count);
            Assert.AreEqual("DealType", results[0].Dim);

            //search for doc that won't exist  
            List<FacetResult> results2 = engine.GetFacetsForQuery(new TermQuery(new Term("ID", Guid.NewGuid().ToString())), null);
            Assert.AreEqual(0, results2.Count);
            Assert.IsFalse(results2.Exists(r => r.Dim == "Category"));
            search.DeleteAll();
            search.Commit();
        }


        [TestMethod()]
        public void SearchEngine_CorrectlyCountsCategoryAndDealTypeFacetsWithNoFilters()
        {
            MarketItem marketItem = new MarketItem
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

            MarketItem marketItem2 = new MarketItem
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

            MarketItem marketItem3 = new MarketItem
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


            SearchEngine engine = new SearchEngine(marketManager, indexDir);

            var results = engine.GetFacetsForAllDocuments();

            Assert.AreEqual(2, results.Find(x => x.Dim.Equals("Category")).ChildCount);
            Assert.AreEqual("Gold",results.Find(x => x.Dim.Equals("Category")).LabelValues[0].Label);
            Assert.AreEqual(2,results.Find(x => x.Dim.Equals("Category")).LabelValues[0].Value);
            Assert.AreEqual("Silver", results.Find(x => x.Dim.Equals("Category")).LabelValues[1].Label);
            Assert.AreEqual(1, results.Find(x => x.Dim.Equals("Category")).LabelValues[1].Value);

            Assert.AreEqual(2, results.Find(x => x.Dim.Equals("Shipping")).ChildCount);
            Assert.AreEqual("International", results.Find(x => x.Dim.Equals("Shipping")).LabelValues[0].Label);
            Assert.AreEqual(2, results.Find(x => x.Dim.Equals("Shipping")).LabelValues[0].Value);
            Assert.AreEqual("Europe", results.Find(x => x.Dim.Equals("Shipping")).LabelValues[1].Label);
            Assert.AreEqual(1, results.Find(x => x.Dim.Equals("Shipping")).LabelValues[1].Value);
           // search.DeleteAll();
        }


        [TestMethod()]
        public void SearchEngine_FindsRhodiumBarBySearchPhrase()
        {
            
            MarketItem marketItem = new MarketItem
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

            MarketItem marketItem2 = new MarketItem
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

            MarketItem marketItem3 = new MarketItem
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

            SearchEngine engine = new SearchEngine(marketManager, indexDir);

            var result = engine.Search(engine.ParseQuery("rhodium bar"));
            var topHit = result.Results[0];

            Assert.AreEqual("1oz Baird & Co Minted Rhodium Bar", topHit.Title);
            Assert.AreEqual(3, result.Results.Count);

           
        }

        [TestMethod()]
        public void SearchEngine_PaginationStartsAtSecondPage()
        {
            string indexDir = "./search";
            var marketManager = Substitute.For<IMarketManager>();
            SearchIndexer search = new SearchIndexer(indexDir, marketManager);
            search.DeleteAll();
            search.Commit();
            for (int i = 0; i < 100; i++)
            {
                MarketItem marketItem = new MarketItem
                {
                    Signature = i.ToString(),
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
                search.Index(marketItem, "block-hash");
            }
            search.Commit();

            SearchEngine engine = new SearchEngine(marketManager, indexDir);

            var result = engine.Search(engine.ParseQuery("rhodium bar"), false, 2);
            var topHit = result.Results[0];

            Assert.AreEqual("21", topHit.Signature);
            Assert.AreEqual(engine.HitsPerPage, result.Results.Count);

        }
       
    }
}