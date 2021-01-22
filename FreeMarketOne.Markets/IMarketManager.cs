using FreeMarketOne.BlockChain;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Pools;
using Libplanet.Extensions;
using System.Collections.Generic;
using System.Net;

namespace FreeMarketOne.Markets
{
    public interface IMarketManager
    {
        List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(byte[] pubKey,
            MarketPoolManager marketPoolManager,
            IBlockChainManager<MarketAction> marketBlockChainManager);
        List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(List<byte[]> userPubKeys,
            MarketPoolManager marketPoolManager,
            IBlockChainManager<MarketAction> marketBlockChainManager);
        List<byte[]> GetBuyerPubKeyFromMarketItem(MarketItemV1 itemMarket);
        MarketItemV1 GetOfferBySignature(string signature,
            MarketPoolManager marketPoolManager,
            IBlockChainManager<MarketAction> marketBlockChainManager);
        List<byte[]> GetSellerPubKeyFromMarketItem(MarketItemV1 itemMarket);
        List<MarketItemV1> GetAllBuyerMarketItemsByPubKeys(byte[] pubKey,
            MarketPoolManager marketPoolManager,
            IBlockChainManager<MarketAction> marketBlockChainManager);
        MarketItemV1 SignBuyerMarketData(
            MarketItemV1 marketData,
            string onionAddress,
            UserPrivateKey privateKey);
        MarketItemV1 SignMarketData(
            MarketItemV1 marketData, UserPrivateKey privateKey);

        List<MarketItemV1> GetAllSellerMarketItemsByPubKeysFromPool(
            List<MarketItemV1> chainMarketItems,
            byte[] userPubKey,
            MarketPoolManager marketPoolManager);

        List<MarketItemV1> GetAllSellerMarketItemsByPubKeysFromPool(
                    List<MarketItemV1> chainMarketItems,
                    List<byte[]> userPubKeys,
                    MarketPoolManager marketPoolManager);

        List<MarketItemV1> GetAllBuyerMarketItemsByPubKeysFromPool(
            List<MarketItemV1> chainMarketItems,
            byte[] userPubKey,
            MarketPoolManager marketPoolManager);

        List<MarketItemV1> GetAllBuyerMarketItemsByPubKeysFromPool(
            List<MarketItemV1> chainMarketItems,
            List<byte[]> userPubKeys,
            MarketPoolManager marketPoolManager);
    }
}