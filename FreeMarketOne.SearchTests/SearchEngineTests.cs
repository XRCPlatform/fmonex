using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Lucene.Net.Facet;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
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
            List<FacetResult> results = engine.GetFacetsForQuery(new TermQuery(new Term("ID", "A")));

            Assert.AreEqual(7, results.Count);
            Assert.AreEqual("DealType", results[0].Dim);

            //search for doc that won't exist  
            List<FacetResult> results2 = engine.GetFacetsForQuery(new TermQuery(new Term("ID", Guid.NewGuid().ToString())));
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

            var result = engine.Search("rhodium bar");
            var topHit = result.Results[0];

            Assert.AreEqual("1oz Baird & Co Minted Rhodium Bar", topHit.Title);
            Assert.AreEqual(3, result.Results.Count);

           
        }
        /// <summary>
        /// TODO: Raise issue to lucene.net team and look for solution
        /// This test is failing due to bug in FacetCollector it seems. So I have commented out the Search accorss dimentions for now and then it gathers facets but does not match on dim names.
        ///  Message: 
        //  Test method FreeMarketOne.Search.Tests.SearchEngineTests.SearchEngine_FindsRhodiumBarBySearchPhraseIncludesManufacturer threw exception: 
        //  System.IndexOutOfRangeException: Index was outside the bounds of the array.
        //  Stack Trace: 
        //  FastTaxonomyFacetCounts.Count(IList`1 matchingDocs)
        //  FastTaxonomyFacetCounts.ctor(String indexFieldName, TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector fc)
        //  FastTaxonomyFacetCounts.ctor(TaxonomyReader taxoReader, FacetsConfig config, FacetsCollector fc)
        //  SearchEngine.GetFacetsForQuery(Query query, List`1 FacetFieldNames) line 65
        //  SearchEngine.GetFacetsForQuery(Query query) line 54
        //  SearchEngine.Search(Query query, Boolean queryFacets, Int32 page) line 141
        //  SearchEngineTests.SearchEngine_FindsRhodiumBarBySearchPhraseIncludesManufacturer() line 267
        /// </summary>
        [TestMethod()]
        public void SearchEngine_FindsRhodiumBarBySearchPhraseIncludesManufacturer()
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
                Description = "The Royal Mint. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Royal Mint. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                Title = "1 Kilogram Gold Cast",
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
                Description = "The 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                Title = "1 Kilogram Gold Cast (C)",
                Shipping = "Europe",
                Manufacturer = "Royal Mint"
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

            var result = engine.Search("Royal Mint", false);
            var topHit = result.Results[0];

            Assert.AreEqual("1 Kilogram Gold Cast (C)", topHit.Title);
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

            var result = engine.Search("rhodium bar", false, 2);
            var topHit = result.Results[0];

            Assert.AreEqual("21", topHit.Signature);
            Assert.AreEqual(engine.HitsPerPage, result.Results.Count);

        }

        [TestMethod()]
        public void SearchEngine_FilterReduceHits()
        {
            string indexDir = "./search";
            var marketManager = Substitute.For<IMarketManager>();
            SearchIndexer search = new SearchIndexer(indexDir, marketManager);
            search.DeleteAll();
            search.Commit();
            for (int i = 0; i < 100; i++)
            {
                int cat = (i + 1) % 10;
                MarketItem marketItem = new MarketItem
                {
                    Signature = i.ToString(),
                    CreatedUtc = DateTime.UtcNow,
                    DealType = 1,
                    Category = cat,
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
            List<Selector> selectors = new List<Selector>();
            selectors.Add(new Selector("Category", "Rhodium"));
            selectors.Add(new Selector("Manufacturer", "Baird & Co"));
            var query = engine.BuildDrillDown(selectors, null);
            var result = engine.Search(query);
       
            Assert.AreEqual(10, result.Results.Count);

        }

        [TestMethod()]
        public void SearchEngine_FilterAndQueryOperateAndReduceHits()
        {
            string indexDir = "./search";
            var marketManager = Substitute.For<IMarketManager>();
            SearchIndexer search = new SearchIndexer(indexDir, marketManager);
            search.DeleteAll();
            search.Commit();
            for (int i = 0; i < 100; i++)
            {
                int cat = (i + 1) % 10;
                MarketItem marketItem = new MarketItem
                {
                    Signature = i.ToString(),
                    CreatedUtc = DateTime.UtcNow,
                    DealType = 1,
                    Category = cat,
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
            List<Selector> selectors = new List<Selector>();
            selectors.Add(new Selector("Category", "Rhodium"));
            selectors.Add(new Selector("Manufacturer", "Baird & Co"));
            var query = engine.BuildDrillDown(selectors, engine.ParseQuery("rhodium bar"));
            var result = engine.Search(query);

            Assert.AreEqual(10, result.Results.Count);

        }


        [TestMethod()]
        public void SearchEngine_FilterRhodiumAndQueryGoldOperateAndReduceHits()
        {
            string indexDir = "./search";
            var marketManager = Substitute.For<IMarketManager>();
            SearchIndexer search = new SearchIndexer(indexDir, marketManager);
            search.DeleteAll();
            search.Commit();
            for (int i = 0; i < 100; i++)
            {
                int cat = (i + 1) % 10;
                MarketItem marketItem = new MarketItem
                {
                    Signature = i.ToString(),
                    CreatedUtc = DateTime.UtcNow,
                    DealType = 1,
                    Category = cat,
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
            List<Selector> selectors = new List<Selector>();
            selectors.Add(new Selector("Category", "Rhodium"));
            selectors.Add(new Selector("Manufacturer", "Baird & Co"));
            var query = engine.BuildDrillDown(selectors, engine.ParseQuery("gold"));
            var result = engine.Search(query);

            Assert.AreEqual(0, result.Results.Count);

        }

    }
}