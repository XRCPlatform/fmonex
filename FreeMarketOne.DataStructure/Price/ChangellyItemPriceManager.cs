using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price
{
    public class ChangellyItemPriceManager : IMarketItemPrice
    {
        public double BasePrice { get; set ; }

        public IEnumerable<IItemPriceResponse> GetItemPriceInExchangedCurrency(Currency[] currency)
        {
            throw new NotImplementedException();
        }
    }
}
