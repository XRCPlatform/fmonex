﻿using FreeMarketOne.BlockChain;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Markets;
using FreeMarketOne.Pools;
using Libplanet.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace FreeMarketOne.SearchTests
{
    public class MoqMarketManager : IMarketManager
    {
        public MoqMarketManager(Dictionary<string,List<byte[]>> publicKeys)
        {
            PublicKeys = publicKeys;
        }

        public Dictionary<string, List<byte[]>> PublicKeys { get; set; }

        public List<MarketItemV1> GetAllBuyerMarketItemsByPubKeys(byte[] pubKey, MarketPoolManager marketPoolManager, IBlockChainManager<MarketAction> marketBlockChainManager)
        {
            throw new NotImplementedException();
        }

        public List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(byte[] pubKey, MarketPoolManager marketPoolManager, IBlockChainManager<MarketAction> marketBlockChainManager)
        {
            throw new NotImplementedException();
        }

        public List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(List<byte[]> userPubKeys, MarketPoolManager marketPoolManager, IBlockChainManager<MarketAction> marketBlockChainManager)
        {
            throw new NotImplementedException();
        }

        public List<byte[]> GetBuyerPubKeyFromMarketItem(MarketItemV1 itemMarket)
        {
            throw new NotImplementedException();
        }

        public MarketItemV1 GetOfferBySignature(string signature, MarketPoolManager marketPoolManager, IBlockChainManager<MarketAction> marketBlockChainManager)
        {
            throw new NotImplementedException();
        }

        public List<byte[]> GetSellerPubKeyFromMarketItem(MarketItemV1 itemMarket)
        {
            return PublicKeys[itemMarket.Signature]; 
        }

        public MarketItemV1 SignBuyerMarketData(MarketItemV1 marketData, IPAddress publicIP, UserPrivateKey privateKey)
        {
            throw new NotImplementedException();
        }

        public MarketItemV1 SignMarketData(MarketItemV1 marketData, UserPrivateKey privateKey)
        {
            throw new NotImplementedException();
        }
    }
}