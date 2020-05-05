using System.Collections.Generic;

namespace FreeMarketOne.DataStructure.Price
{
    public interface IMarketItemPrice
    {
        decimal BasePrice { get;}
        IEnumerable<IItemPriceResponse> GetItemPriceInExchangedCurrency(Currency[] currency);
    }
}
