using FreeMarketOne.DataStructure.Objects.BaseItems;
using System.Collections.Generic;

namespace FreeMarketOne.ServerCore
{
    public interface IMarketManager
    {
        //List<MarketItemV1> GetAllActiveOffers(string category = "All"); 
        List<MarketItem> GetAllSellerMarketItemsByPubKeys(byte[] pubKey);
        List<MarketItem> GetAllSellerMarketItemsByPubKeys(List<byte[]> userPubKeys);
        List<byte[]> GetBuyerPubKeyFromMarketItem(MarketItem itemMarket);
        MarketItem GetOfferBySignature(string signature);
        List<byte[]> GetSellerPubKeyFromMarketItem(MarketItem itemMarket);
    }
}