using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.DataStructure.Price
{
    public interface IMarketItemPrice
    {
        decimal BasePrice { get;}
        IEnumerable<IItemPriceResponse> GetItemPriceInExchangedCurrency(Currency[] currency);
    }
}
