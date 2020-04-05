using FreeMarketOne.DataStructure.Price.ChangellyApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace FreeMarketOne.DataStructure.Test
{
    [TestClass]
    public class ChangellyApiClientTest
    {
        [TestMethod]
        public void GetExchangeAmount()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var resp = changelly.GetExchangeAmount(Price.Currency.XRC, 
                new Price.Currency[] 
                    { 
                        Price.Currency.BTC, 
                        Price.Currency.USDT 
                    }
            , 10);

            Assert.AreEqual(resp.result[0].Amount ,10);
            Assert.IsTrue(string.Equals(resp.result[0].To, "btc"));
        }

        [TestMethod]
        public void GetCurrenciesFull()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.GetCurrenciesFull();
            bool foundXrc = false;
            bool xrcIsEnabled = false;
            foreach (var item in response.result)
            {
                if (item.Ticker.Equals("xrc",System.StringComparison.InvariantCultureIgnoreCase))
                {
                    foundXrc = true;
                    xrcIsEnabled = item.Enabled;
                    break;
                }
            }
            Assert.IsTrue(response.result.Length > 0);
            Assert.IsTrue(foundXrc);
            Assert.IsTrue(xrcIsEnabled);
        }

        [TestMethod]
        public void GetMinAmount()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.GetMinAmount(Price.Currency.LTC, Price.Currency.BTC);
            Assert.IsTrue(response.result.Length>0);
            Assert.IsTrue(response.result[0].MinAmount>0);
        }

        [TestMethod]
        public void GetMinAmountXRC2BTC()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.GetMinAmount(Price.Currency.XRC, Price.Currency.BTC);
            Assert.IsTrue(response.result.Length > 0);
            Assert.IsTrue(response.result[0].MinAmount > 0);
        }

        [TestMethod]
        public void ValidateAddress_Invalid()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.ValidateAddress(Price.Currency.XRC, "invalid-xrc-adddress");
            Assert.IsFalse(response);
        }

        [TestMethod]
        public void ValidateAddress_Valid()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.ValidateAddress(Price.Currency.XRC, "RZx6ZCoizGTamQatK8cSZkhdBU3iHkrXEW");
            Assert.IsTrue(response);
        }
    }
}
