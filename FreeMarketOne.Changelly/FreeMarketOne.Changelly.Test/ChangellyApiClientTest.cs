using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Price;
using FreeMarketOne.ServerCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;


namespace FreeMarketOne.Changelly.Test
{
    [TestClass]
    public class ChangellyApiClientTest
    {
        [TestMethod]
        public void GetExchangeAmount()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var resp = changelly.GetExchangeAmount(Currency.XRC, 
                new Currency[] 
                    { 
                        Currency.BTC, 
                        Currency.USDT 
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
                if (item.Ticker.Equals("xrc", StringComparison.InvariantCultureIgnoreCase))
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
            var response = changelly.GetMinAmount(Currency.LTC, new Currency[1] { Currency.BTC });
            Assert.IsTrue(response.result.Length>0);
            Assert.IsTrue(response.result[0].MinAmount>0);
        }

        [TestMethod]
        public void GetMinAmountXRC2BTC()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.GetMinAmount(Currency.XRC, new Currency[2] { Currency.BTC, Currency.USDT });;
            Assert.IsTrue(response.result.Length > 0);
            Assert.IsTrue(response.result[0].MinAmount > 0);
        }

        [TestMethod]
        public void ValidateAddress_Invalid()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.ValidateAddress(Currency.XRC, "invalid-xrc-adddress");
            Assert.IsFalse(response);
        }

        [TestMethod]
        public void ValidateAddress_Valid()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.ValidateAddress(Currency.XRC, "RZx6ZCoizGTamQatK8cSZkhdBU3iHkrXEW");
            Assert.IsTrue(response);
        }


        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void CreateTransaction_throws_invalid_amount_exception()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.CreateTransaction(Currency.XRC, Currency.BTC, "31h8jnstG777RfqFugM1vLLT7pPrFQsr3x", "Rp2mdiL2AUvDzRnt8qPY2ePUbjmcFxdt9d",0.0001m);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "Invalid currency pair")]
        public void CreateTransaction_throws_invalid_currency_pair_exception()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.CreateTransaction(Currency.XRC, Currency.XRC, "31h8jnstG777RfqFugM1vLLT7pPrFQsr3x", "Rp2mdiL2AUvDzRnt8qPY2ePUbjmcFxdt9d", 2.9m);
        }

        [TestMethod]
        public void CreateTransaction()
        {
            ChangellyApiClient changelly = new ChangellyApiClient(new MainConfiguration());
            var response = changelly.CreateTransaction(Currency.XRC, Currency.BTC, "31h8jnstG777RfqFugM1vLLT7pPrFQsr3x", "Rp2mdiL2AUvDzRnt8qPY2ePUbjmcFxdt9d", 2.9m);
            Assert.IsTrue(response.id.Length > 0);
            Assert.IsTrue(response.status.Equals("new", StringComparison.InvariantCultureIgnoreCase));
            Assert.IsTrue(response.payinAddress.Length == 34);
        }

    }
}
