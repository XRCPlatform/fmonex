using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price
{
    public class ChangellyItemPriceManager : IMarketItemPrice
    {
        private double basePrice;
        public ChangellyItemPriceManager(double basePriceInXrc)
        {
            basePrice = basePriceInXrc;
        }

        public double BasePrice { get { return basePrice; } }

        public IEnumerable<IItemPriceResponse> GetItemPriceInExchangedCurrency(Currency[] currency)
        {
            throw new NotImplementedException();
        }
    }
}
