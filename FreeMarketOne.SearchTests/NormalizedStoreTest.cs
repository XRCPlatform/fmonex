using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Search;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeMarketOne.SearchTests
{
    public class NormalizedStoreTest
    {
        [TestClass()]
        public class SearchEngineTests
        {
            private static string dbPath;
            private static IBaseConfiguration config;
  

            [ClassInitialize()]
            public static void ClassInit(TestContext context)
            {
                var indexDir = "normalized_store";
                config = Substitute.For<IBaseConfiguration>();
                config.SearchEnginePath.Returns(indexDir);
                config.FullBaseDirectory.Returns(Environment.CurrentDirectory);
                dbPath = Path.Join(config.FullBaseDirectory, config.SearchEnginePath);

                if (System.IO.Directory.Exists(dbPath))
                {
                    System.IO.Directory.Delete(dbPath, true);
                }
            }

            [TestMethod()]
            public void NormalizedStore_CorrectlySavesAndRetrievesMarketItem()
            {
                var normalizedStore = new NormalizedStore(dbPath);
                MarketItemV1 marketItem = new MarketItemV1
                {
                    Signature = Guid.NewGuid().ToString(),
                    CreatedUtc = DateTime.UtcNow,
                    DealType = 3,
                    Category = 2,
                    Price = 10868.4F,
                    BuyerSignature = "a",
                    Description = "The Baird & Co. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                    Title = "1 Kilogram Gold Cast Bar Baird & Co",
                    Shipping = "International"
                };

                normalizedStore.Save(marketItem, OfferDirection.Sold);
                var result = normalizedStore.GetOfferById(marketItem.Signature);
                Assert.AreEqual(result.Signature, marketItem.Signature);
            }

            [TestMethod()]
            public void NormalizedStore_CorrectlySavesMultipleMarketItemsAndRetrievesPaged()
            {
                var normalizedStore = new NormalizedStore(dbPath);
                MarketItemV1 marketItem = null;
                for (int i = 1; i < 20; i++)
                {
                    marketItem = new MarketItemV1
                    {
                        Signature = i.ToString(),
                        CreatedUtc = DateTime.UtcNow,
                        DealType = 3,
                        Category = 2,
                        Price = 10868.4F,
                        BuyerSignature = "a",
                        Description = "The Baird & Co. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                        Title = "1 Kilogram Gold Cast Bar Baird & Co",
                        Shipping = "International"
                    };

                    normalizedStore.Save(marketItem, OfferDirection.Sold);
                }
     
                var result = normalizedStore.GetMyOffers(OfferDirection.Sold, 3, 7);
                Assert.AreEqual(1,result.Results.Count());
                Assert.AreEqual("1",result.Results.FirstOrDefault().Signature);
            }

            [TestMethod()]
            public void NormalizedStore_CorrectlyHandlesPagination()
            {
                var normalizedStore = new NormalizedStore(dbPath);
                MarketItemV1 marketItem = null;
                for (int i = 0; i < 20; i++)
                {
                    marketItem = new MarketItemV1
                    {
                        Signature = i.ToString(),
                        CreatedUtc = DateTime.UtcNow,
                        DealType = 3,
                        Category = 2,
                        Price = 10868.4F,
                        BuyerSignature = "a",
                        Description = "The Baird & Co. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                        Title = "1 Kilogram Gold Cast Bar Baird & Co",
                        Shipping = "International"
                    };

                    normalizedStore.Save(marketItem, OfferDirection.Sold);
                }

                var result = normalizedStore.GetMyOffers(OfferDirection.Sold, 5, 1);
                
                Assert.AreEqual(20, result.TotalHits);

                Assert.AreEqual(5, result.Results.Count());
                Assert.AreEqual("19",result.Results.FirstOrDefault().Signature);

                var result1 = normalizedStore.GetMyOffers(OfferDirection.Sold, 5, 2);
                Assert.AreEqual(5, result1.Results.Count());
                Assert.AreEqual("9", result1.Results.FirstOrDefault().Signature);

                var result2 = normalizedStore.GetMyOffers(OfferDirection.Sold, 5, 3);
                Assert.AreEqual(5, result2.Results.Count());
                Assert.AreEqual("4",result2.Results.FirstOrDefault().Signature);

                var result3 = normalizedStore.GetMyOffers(OfferDirection.Sold, 5, 4);
                Assert.AreEqual(0, result3.Results.Count());                
            }

            [TestMethod()]
            public void NormalizedStore_GetOffersByWrongDirectionReturns0()
            {
                var normalizedStore = new NormalizedStore(dbPath);
                MarketItemV1 marketItem = null;
                for (int i = 1; i < 20; i++)
                {
                    marketItem = new MarketItemV1
                    {
                        Signature = i.ToString(),
                        CreatedUtc = DateTime.UtcNow,
                        DealType = 3,
                        Category = 2,
                        Price = 10868.4F,
                        BuyerSignature = "a",
                        Description = "The Baird & Co. 1kg gold cast bar is produced at our internationally recognised London refinery. This bar is produced to the internationally recognised 999.9 standard and carries the Baird & Co. mark, the weight, fineness and a unique serial number. PLEASE NOTE THAT DUE TO THE INCREASE IN THE GOLD PRICE WE ARE UNABLE TO DISPATCH KILO BARS AS IT EXCEEDS INSURANCE LIMITS. BARS CAN STILL BE COLLECTED OR PLACED INTO AN ALLOCATED ACCOUNT",
                        Title = "1 Kilogram Gold Cast Bar Baird & Co",
                        Shipping = "International"
                    };

                    normalizedStore.Save(marketItem, OfferDirection.Bought);
                }

                var result = normalizedStore.GetMyOffers(OfferDirection.Sold, 10, 1);
                Assert.AreEqual(0, result.Results.Count());
            }
        }
    }
}
