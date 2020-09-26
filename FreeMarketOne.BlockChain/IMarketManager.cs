using FreeMarketOne.DataStructure.Objects.BaseItems;
using System.Collections.Generic;

namespace FreeMarketOne.ServerCore
{
    public interface IMarketManager
    {
        //List<MarketItemV1> GetAllActiveOffers(string category = "All"); 
        List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(byte[] pubKey);
        List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(List<byte[]> userPubKeys);
        List<byte[]> GetBuyerPubKeyFromMarketItem(MarketItemV1 itemMarket);
        MarketItemV1 GetOfferBySignature(string signature);
        List<byte[]> GetSellerPubKeyFromMarketItem(MarketItemV1 itemMarket);
    }
}