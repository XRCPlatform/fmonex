using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price
{
    public interface IMarketItemPrice
    {
        double BasePrice { get; set; }
        IEnumerable<IItemPriceResponse> GetItemPriceInExchangedCurrency(Currency[] currency);
    }
}
