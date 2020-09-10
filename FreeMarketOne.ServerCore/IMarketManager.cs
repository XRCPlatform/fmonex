using FreeMarketOne.DataStructure.Objects.BaseItems;
using System.Collections.Generic;

namespace FreeMarketOne.ServerCore
{
    public interface IMarketManager
    {
        List<MarketItemV1> GetAllActiveOffers(MarketManager.MarketCategoryEnum category = MarketManager.MarketCategoryEnum.All);
        List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(byte[] pubKey);
        List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(List<byte[]> userPubKeys);
        List<byte[]> GetBuyerPubKeyFromMarketItem(MarketItem itemMarket);
        MarketItemV1 GetOfferBySignature(string signature);
        List<byte[]> GetSellerPubKeyFromMarketItem(MarketItem itemMarket);
    }
}