using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Price;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FreeMarketOne.Changelly
{
    public class ChangellyItemPriceManager : IMarketItemPrice
    {

        //need singleton for api so that caches and etc are preserved 
        private static ChangellyApiClient changellyApiClient;
        private readonly object constructorLock = new object();

        public ChangellyItemPriceManager(decimal basePriceInXrc, IBaseConfiguration config)
        {
            BasePrice = basePriceInXrc;
            lock (constructorLock)
            {
                if (changellyApiClient == null)
                {
                    changellyApiClient = new ChangellyApiClient(config);
                }
            }
        }

        public decimal BasePrice { get; }

        public IEnumerable<IItemPriceResponse> GetItemPriceInExchangedCurrency(Currency[] currencies)
        {
            List<ItemPriceResponse> itemPrices = new List<ItemPriceResponse>();
            List<ItemPriceResponse> itemPricesFinal = new List<ItemPriceResponse>();

            var mins = changellyApiClient.GetMinAmount(Currency.XRC, currencies).result;
            
            //remove exchanges that will be below min amount
            Currency[] filtered = RemoveCurrenciesBelowMinExchangeAmount(currencies, mins, itemPricesFinal);

            if (filtered.Length > 0)
            {
                var response = changellyApiClient.GetExchangeAmount(Currency.XRC, filtered, BasePrice);
                foreach (var item in response.result)
                {
                    decimal minAmount = 0;
                    if (mins != null)
                    {
                        foreach (var min in mins)
                        {
                            if (min.To.Equals(item.To, StringComparison.InvariantCultureIgnoreCase))
                            {
                                minAmount = min.MinAmount;
                                break;
                            }
                        }
                    }

                    object exCurrency = null;
                    if (Enum.TryParse(typeof(Currency), item.To.ToUpper(), out exCurrency))
                    {
                        itemPrices.Add(new ItemPriceResponse()
                        {
                            Amount = item.Result,
                            Currency = (Currency)exCurrency,
                            Fee = item.Fee,
                            Rate = item.Rate,
                            MinAmount = minAmount
                        });
                    }
                }
            }

            
            if (itemPrices.Count < currencies.Length && itemPricesFinal.Count>0)
            {
                //add missing minimum response.
                foreach (var item in currencies)
                {
                    var r = itemPrices.Where(x => x.Currency.Equals(item));
                    if (r.Any())
                    {
                        itemPricesFinal.Add(r.FirstOrDefault());
                    }
                }
            }
            else
            {
                itemPricesFinal = itemPrices;
            }

            return itemPricesFinal;
        }

        private Currency[] RemoveCurrenciesBelowMinExchangeAmount(Currency[] currencies, GetMinamountResponseResult[] mins, List<ItemPriceResponse> final)
        {
            var currencies_list = new List<Currency>(currencies);
            if (mins != null)
            {
                foreach (var min in mins)
                {
                    for (int i = 0; i < currencies_list.Count; i++)
                    {

                        if (min.To.Equals(currencies_list[i].ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (min.MinAmount > BasePrice)
                            {
                                final.Add(new ItemPriceResponse()
                                {
                                    Amount = 0,
                                    Currency = currencies_list[i],
                                    Fee = 0,
                                    Rate = 0,
                                    MinAmount = min.MinAmount
                                });
                                currencies_list.RemoveAt(i);
                            }
                        }
                    }
                }
            }

            return currencies_list.ToArray();
        }
    }
}
