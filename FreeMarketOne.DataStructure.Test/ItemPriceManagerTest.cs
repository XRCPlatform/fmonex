using FreeMarketOne.DataStructure.Price;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Test
{
    [TestClass]
    public class ItemPriceManagerTest
    {
        [TestMethod]
        public void GetExchangeAmount_WithBelowMinAmount()
        {
            IMarketItemPrice changellyMgr = new ChangellyItemPriceManager(1.99m, new MainConfiguration());
            var resp = changellyMgr.GetItemPriceInExchangedCurrency
                (
                    new Currency[]
                    {
                        Currency.BTC,
                        Currency.USDT
                    }
                );

            foreach (var item in resp)
            {
                Assert.IsTrue(item.MinAmount>0);
                Assert.IsTrue(item.Rate == 0);
            }
               
        }

        [TestMethod]
        public void GetExchangeAmount()
        {
            IMarketItemPrice changellyMgr = new ChangellyItemPriceManager(8.99m, new MainConfiguration());
            var resp = changellyMgr.GetItemPriceInExchangedCurrency
                (
                    new Currency[]
                    {
                        Currency.BTC,
                        Currency.USDT
                    }
                );

            foreach (var item in resp)
            {
                Assert.IsTrue(item.MinAmount > 0);
                Assert.IsTrue(item.Rate > 0);
                Assert.IsTrue(item.Amount > 0);
            }

        }
        /// <summary>
        /// if the exchange rate changes the logic will change
        /// probably need a stub for api client so that can isolate and test logic
        /// </summary>
        [TestMethod]
        public void GetExchangeAmountPartialWithMininmums()
        {
            IMarketItemPrice changellyMgr = new ChangellyItemPriceManager(3m, new MainConfiguration());
            var resp = changellyMgr.GetItemPriceInExchangedCurrency
                (
                    new Currency[]
                    {
                        Currency.BTC,
                        Currency.USDT
                    }
                );

            foreach (var item in resp)
            {
                Assert.IsTrue(item.MinAmount > 0);
                // only give quotes for viable exchange amounts 
                // below minimum will have 0s
                if (item.Currency.Equals(Currency.BTC))
                {
                    Assert.IsTrue(item.Rate > 0);
                    Assert.IsTrue(item.Amount > 0);
                }
                else
                {
                    Assert.IsTrue(item.Rate == 0);
                    Assert.IsTrue(item.Amount == 0);
                }
               
            }

        }
    }
}
