﻿using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Markets;
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
        private static SearchIndexer search;
        private static IMarketManager marketManager;
        private static IXRCHelper xrcHelper;
        private static string indexDir;

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            indexDir = "./search";
            marketManager = Substitute.For<IMarketManager>();
            xrcHelper = Substitute.For<IXRCHelper>();
            xrcHelper.GetTransaction(Arg.Any<string>(), Arg.Any<string>()).Returns(new XRCTransactionSummary()
            {
                Confirmations = 10,
                Date = DateTimeOffset.UtcNow.AddMinutes(-50),
                Total = new Random(int.MaxValue).Next()
            });

            search = new SearchIndexer(marketManager, indexDir, xrcHelper);
        }

        [TestMethod()]
        public void SearchEngine_CorrectlyCountsFacetsOnTermQuery()
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

        }


        [TestMethod()]
        public void SearchEngine_FindsRhodiumBarBySearchPhrase()
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
       
        [TestMethod()]
        public void SearchEngine_FindsRhodiumBarBySearchPhraseIncludesManufacturer()
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
                Description = "The Royal Mint. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Royal Mint. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                Title = "1 Kilogram Gold Cast",
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
                Description = "The 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                Title = "1 Kilogram Gold Cast (C)",
                Shipping = "Europe",
                Manufacturer = "Royal Mint"
            };

            search.DeleteAll();
            search.Commit();
            search.Index(marketItem, "block-hash");
            search.Index(marketItem2, "block-hash");
            search.Index(marketItem3, "block-hash");
            search.Commit();

            SearchEngine engine = new SearchEngine(marketManager, indexDir);

            var result = engine.Search("Royal Mint", true);
            var topHit = result.Results[0];

            Assert.AreEqual("1 Kilogram Gold Cast (C)", topHit.Title);
            Assert.AreEqual(3, result.Results.Count);


        }

        [TestMethod()]
        public void SearchEngine_PaginationStartsAtSecondPage()
        {
            search.DeleteAll();
            search.Commit();
            for (int i = 0; i < 100; i++)
            {
                MarketItemV1 marketItem = new MarketItemV1
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

            Assert.IsTrue(int.Parse(topHit.Signature)>20 && int.Parse(topHit.Signature)<30);
            Assert.AreEqual(engine.PageSize, result.Results.Count);

        }

        [TestMethod()]
        public void SearchEngine_FilterReduceHits()
        {
            search.DeleteAll();
            search.Commit();
            for (int i = 0; i < 100; i++)
            {
                int cat = (i + 1) % 10;
                MarketItemV1 marketItem = new MarketItemV1
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
            search.DeleteAll();
            search.Commit();
            for (int i = 0; i < 100; i++)
            {
                int cat = (i + 1) % 10;
                MarketItemV1 marketItem = new MarketItemV1
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
            search.DeleteAll();
            search.Commit();
            for (int i = 0; i < 100; i++)
            {
                int cat = (i + 1) % 10;
                MarketItemV1 marketItem = new MarketItemV1
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


        [TestMethod()]
        public void SearchEngine_SearchBySellerPubKeys()
        {
            string indexDir2 = "search2";
            var marketManager1 = Substitute.For<IMarketManager>();
            var search1 = new SearchIndexer(marketManager1, indexDir2, xrcHelper);

            List<ValueTuple<MarketItem, List<byte[]>>> lst = new List<ValueTuple<MarketItem, List<byte[]>>>();

            search1.DeleteAll();
            search1.Commit();
            for (int i = 0; i < 3; i++)
            {
  
                int cat = (i + 1) % 10;
                MarketItemV1 marketItem = new MarketItemV1
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


                List<byte[]> pubKeys = new List<byte[]>();
                for (int z = 0; z < 3; z++)
                {
                    pubKeys.Add(Guid.NewGuid().ToByteArray());
                }
                //setup stub
                marketManager1.GetSellerPubKeyFromMarketItem(marketItem).Returns(pubKeys);

                var t = (marketItem, pubKeys);
                lst.Add(t);               
                
                //index
                search1.Index(marketItem, "block-hash");
            }
            search1.Commit();
            search1.Dispose();

            SearchEngine engine = new SearchEngine(marketManager1, indexDir2);

            var testcase = lst[0];
            var query = engine.BuildQueryBySellerPubKeys(testcase.Item2);
            var result = engine.Search(query);

            Assert.AreEqual(testcase.Item1.Signature,result.Results[0].Signature);

        }



        [TestMethod()]
        public void SearchEngine_SearchBySignature()
        {
            search.DeleteAll();
            search.Commit();
            for (int i = 0; i < 15; i++)
            {
                int cat = (i + 1) % 10;
                MarketItemV1 marketItem = new MarketItemV1
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
            string signature = "10";
            var query = engine.BuildQueryBySignature(signature);
            var result = engine.Search(query);

            Assert.AreEqual(1, result.Results.Count);
            Assert.AreEqual(signature, result.Results[0].Signature);

        }

        [TestMethod()]
        public void SearchEngine_RelevanceRankByXRCTransactionVolume()
        {
            search.DeleteAll();
            search.Commit();
            var marketManager1 = Substitute.For<IMarketManager>();
            xrcHelper = Substitute.For<IXRCHelper>();
            string indexDir = "search2";
            var search1 = new SearchIndexer(marketManager1, indexDir, xrcHelper);
            bool generateSellerKeys = true;
            List<byte[]> pubKeys = new List<byte[]>();

            for (int i = 0; i < 15; i++)
            {
                int cat = (i + 1) % 10;
                string xrcAddress = "RpPrTiDj6rEYrGCxyBYGe18TCWkoMwyMhj" + i;
                string xrcTransactionHash = "cd484fccea9f886ee019f15193417b272a74300d2e30e229eabf262fccf0ee26" + i;

                xrcHelper.GetTransaction(xrcAddress, xrcTransactionHash).Returns(new XRCTransactionSummary()
                {
                    Confirmations = 10,
                    Date = DateTimeOffset.UtcNow.AddMinutes(-50),
                    Total = 1000 * (i+1)
                });           

                MarketItemV1 marketItem = new MarketItemV1
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
                    XRCReceivingAddress = xrcAddress,
                    XRCTransactionHash = xrcTransactionHash
                };

                // assign every third offer new seller
                if (i % 3 == 0)
                {
                    generateSellerKeys = true;
                }
                else
                {
                    generateSellerKeys = false;
                }

                if (generateSellerKeys)
                {
                    pubKeys = new List<byte[]>();
                    for (int z = 0; z < 3; z++)
                    {
                        pubKeys.Add(Guid.NewGuid().ToByteArray());
                    }                    
                }

                marketManager1.GetSellerPubKeyFromMarketItem(marketItem).Returns(pubKeys);
                search1.Index(marketItem, "block-hash");
            }
            search.Commit();

            SearchEngine engine = new SearchEngine(marketManager, indexDir);

            XRCTransactionScoreQuery msm = new XRCTransactionScoreQuery(engine.ParseQuery("rhodium"));
            var result = engine.Search(msm);

            Assert.AreEqual(15, result.Results.Count);
            Assert.AreEqual("14", result.Results[0].Signature);

        }



    }
}