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
        [ExpectedException(typeof(Exception))]
        public void GetExchangeAmount_WithBelowMinAmount()
        {
            IMarketItemPrice changellyMgr = new ChangellyItemPriceManager(1.99, new MainConfiguration());
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
                Assert.IsTrue(item.Rate > 0);
            }
               
        }

        [TestMethod]
        public void GetExchangeAmount()
        {
            IMarketItemPrice changellyMgr = new ChangellyItemPriceManager(8.99, new MainConfiguration());
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
    }
}
