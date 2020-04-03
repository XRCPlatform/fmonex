using FreeMarketOne.DataStructure.Price.ChangellyApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FreeMarketOne.DataStructure.Test
{
    [TestClass]
    public class ChangellyApiClientTest
    {
        [TestMethod]
        public void GetExchangeAmount()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            //today xrc is disabled so test need to run against BTC as base
            var resp = changelly.GetExchangeAmountAsync(Price.Currency.BTC, 
                new Price.Currency[] 
                    { 
                        Price.Currency.LTC, 
                        Price.Currency.USDT 
                    }
            , 10);

            Assert.AreEqual(resp.result[0].Amount ,10);
            Assert.IsTrue(string.Equals(resp.result[0].To, "ltc"));
        }

        [TestMethod]
        public void GetCurrenciesFull()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.GetCurrenciesFull();
            Assert.IsTrue(response.result.Length > 0);
        }

        [TestMethod]
        public void GetMinAmount()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.GetMinAmount(Price.Currency.LTC, Price.Currency.BTC);
            Assert.IsTrue(response.result.Length>0);
            Assert.IsTrue(response.result[0].MinAmount>0);
        }
    }
}
