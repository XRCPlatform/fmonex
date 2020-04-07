using FreeMarketOne.DataStructure.Price.ChangellyApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price
{
    public class ChangellyItemPriceManager : IMarketItemPrice
    {
        private double basePrice;
        
        //need singleton for api so that caches and etc are preserved 
        private static ChangellyApiClient changellyApiClient;
        private readonly object constructorLock = new object();
        
        public ChangellyItemPriceManager(double basePriceInXrc, IBaseConfiguration config)
        {
            basePrice = basePriceInXrc;
            lock (constructorLock)
            {
                if (changellyApiClient == null)
                {
                    changellyApiClient = new ChangellyApiClient(config);
                }
            }
        }

        public double BasePrice { get { return basePrice; } }

        public IEnumerable<IItemPriceResponse> GetItemPriceInExchangedCurrency(Currency[] currencies)
        {
            List<ItemPriceResponse> itemPrices = new List<ItemPriceResponse>();
            var mins = changellyApiClient.GetMinAmount(Currency.XRC, currencies).result;
            var response = changellyApiClient.GetExchangeAmount(Currency.XRC, currencies, basePrice);
            foreach (var item in response.result)
            {
                double minAmount = 0;
                foreach (var min in mins)
                {
                    if(min.To.Equals(item.To, StringComparison.InvariantCultureIgnoreCase))
                    {
                        minAmount = min.MinAmount;
                        break;
                    }
                }
                object exCurrency = null; 
                if (Enum.TryParse(typeof(Currency), item.To.ToUpper(), out exCurrency))
                {
                    itemPrices.Add(new ItemPriceResponse()
                    {
                        Amount = item.Amount,
                        Currency = (Currency) exCurrency,
                        Fee = item.Fee,
                        Rate = item.Rate,
                        MinAmount = minAmount
                    });
                }                
            }
            return itemPrices;
        }
    }
}
