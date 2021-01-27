using FreeMarketOne.BlockChain;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Common;
using FreeMarketOne.Pools;
using Libplanet.Extensions;
using Libplanet.Store;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace FreeMarketOne.Markets
{
    public class MarketManager : IMarketManager
    {
        public enum MarketCategoryEnum
        {
            All = 0,
            Gold = 1,
            Silver = 2,
            Platinum = 3,
            Rhodium = 4,
            Palladium = 5,
            Copper = 6,
            RareCoins = 7,
            Jewelry = 8,
            Cryptocurrency = 9
        }

        public enum ProductPriceTypeEnum
        {
            XRC = 0,
            USD = 1
        }

        public enum ProductStateEnum
        {
            Default = 0,
            Sold = 1,
            Removed = 2
        }

        private IBaseConfiguration _configuration;

        private ILogger _logger { get; set; }

        private readonly object _locked = new object();

        /// <summary>
        /// Inicialization of market manager
        /// </summary>
        /// <param name="configuration"></param>
        public MarketManager(IBaseConfiguration configuration)
        {
            _logger = Log.Logger.ForContext<MarketManager>();
            _logger.Information("Initializing Market Manager");

            _configuration = configuration;
        }

        /// <summary>
        /// Get public key of buyer from market item
        /// </summary>
        /// <param name="itemMarket"></param>
        /// <returns></returns>
        public List<byte[]> GetBuyerPubKeyFromMarketItem(MarketItemV1 itemMarket)
        {
            if (!string.IsNullOrEmpty(itemMarket.BuyerSignature))
            {
                _logger.Information(string.Format("Recovering buyer public key for item hash {0}.", itemMarket.Hash));

                var itemMarketBytes = itemMarket.ToByteArrayForSign();
                return UserPublicKey.Recover(itemMarketBytes, itemMarket.BuyerSignature);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get public key of seller from market item
        /// </summary>
        /// <param name="itemMarket"></param>
        /// <returns></returns>
        public List<byte[]> GetSellerPubKeyFromMarketItem(MarketItemV1 itemMarket)
        {
            if (!string.IsNullOrEmpty(itemMarket.Signature))
            {
                _logger.Information(string.Format("Recovering seller public key for item hash {0}.", itemMarket.Hash));

                var itemMarketBytes = itemMarket.ToByteArrayForSign();
                return UserPublicKey.Recover(itemMarketBytes, itemMarket.Signature);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get all seller market items with old data filtration
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(byte[] pubKey,
            MarketPoolManager marketPoolManager,
            IBlockChainManager<MarketAction> marketBlockChainManager)
        {
            return GetAllSellerMarketItemsByPubKeys(new List<byte[]> { pubKey }, marketPoolManager, marketBlockChainManager);
        }

        /// <summary>
        /// Get all seller market items with old data filtration
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(
            List<byte[]> userPubKeys,
            MarketPoolManager marketPoolManager,
            IBlockChainManager<MarketAction> marketBlockChainManager)
        {
            lock (_locked)
            {
                _logger.Information(string.Format("GetAllSellerMarketItemsByPubKeys."));

                var result = new List<MarketItemV1>();
                var types = new Type[] { typeof(MarketItemV1) };

                var ignoredSignatures = new List<string>();

                var marketStorage = marketBlockChainManager.Storage;
                var chainId = marketStorage.GetCanonicalChainId();
                var countOfIndex = marketStorage.CountIndex(chainId.Value);

                //checking pool
                var poolItems = marketPoolManager.GetAllActionItemByType(types);
                if (poolItems.Any())
                {
                    _logger.Information(string.Format("Some offers found in pool. Loading them too."));
                    poolItems.Reverse();

                    foreach (var itemPool in poolItems)
                    {
                        var marketData = (MarketItemV1)itemPool;

                        if (!ignoredSignatures.Exists(a => a == marketData.Signature))
                        {
                            var itemMarketBytes = marketData.ToByteArrayForSign();
                            var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, marketData.Signature);

                            foreach (var itemPubKey in itemPubKeys)
                            {
                                foreach (var itemUserPubKey in userPubKeys)
                                {
                                    if (itemPubKey.SequenceEqual(itemUserPubKey))
                                    {
                                        if (marketData.State != (int)ProductStateEnum.Removed)
                                        {
                                            _logger.Information(string.Format("Found Pool MarketItem for seller - item hash {0}.", marketData.Hash));
                                            marketData.IsInPool = true;
                                            result.Add(marketData);
                                        }
                                        else
                                        {
                                            _logger.Information(string.Format("Found deleted Pool MarketItem for seller - item hash {0}.", marketData.Hash));
                                        }

                                        var chainSignatures = GetChainSignaturesForMarketItem(
                                                marketStorage, chainId, countOfIndex, marketData, types);

                                        ignoredSignatures.AddRange(chainSignatures);
                                    }
                                }
                            }
                        }
                    }
                }

                //checking blockchain
                for (long i = (countOfIndex - 1); i >= 0; i--)
                {
                    var blockHashId = marketStorage.IndexBlockHash(chainId.Value, i);
                    var block = marketStorage.GetBlock<MarketAction>(blockHashId.Value);

                    foreach (var itemTx in block.Transactions)
                    {
                        foreach (var itemAction in itemTx.Actions)
                        {
                            foreach (var itemMarket in itemAction.BaseItems)
                            {
                                if (types.Contains(itemMarket.GetType()))
                                {
                                    var marketData = (MarketItemV1)itemMarket;

                                    if (!ignoredSignatures.Exists(a => a == marketData.Signature))
                                    {
                                        var itemMarketBytes = itemMarket.ToByteArrayForSign();
                                        var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, itemMarket.Signature);

                                        foreach (var itemPubKey in itemPubKeys)
                                        {
                                            foreach (var itemUserPubKey in userPubKeys)
                                            {
                                                if (itemPubKey.SequenceEqual(itemUserPubKey))
                                                {
                                                    if (marketData.State != (int)ProductStateEnum.Removed)
                                                    {
                                                        _logger.Information(string.Format("Found MarketItem for seller - item hash {0}.", itemMarket.Hash));
                                                        marketData.IsInPool = false;
                                                        result.Add(marketData);
                                                    }
                                                    else
                                                    {
                                                        _logger.Information(string.Format("Found deleted MarketItem for seller - item hash {0}.", itemMarket.Hash));
                                                    }

                                                    var chainSignatures = GetChainSignaturesForMarketItem(
                                                            marketStorage, chainId, countOfIndex, marketData, types);

                                                    ignoredSignatures.AddRange(chainSignatures);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
        }


        /// <summary>
        /// Get all buyer market items with old data filtration
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllBuyerMarketItemsByPubKeys(byte[] pubKey,
            MarketPoolManager marketPoolManager,
            IBlockChainManager<MarketAction> marketBlockChainManager)
        {
            return GetAllBuyerMarketItemsByPubKeys(new List<byte[]> { pubKey }, marketPoolManager, marketBlockChainManager);
        }

        /// <summary>
        /// Get all buyer market items with old data filtration
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllBuyerMarketItemsByPubKeys(List<byte[]> userPubKeys,
            MarketPoolManager marketPoolManager,
            IBlockChainManager<MarketAction> marketBlockChainManager)
        {
            lock (_locked)
            {
                _logger.Information(string.Format("GetAllBuyerMarketItemsByPubKeys."));

                var result = new List<MarketItemV1>();
                var types = new Type[] { typeof(MarketItemV1) };

                var marketStorage = marketBlockChainManager.Storage;
                var chainId = marketStorage.GetCanonicalChainId();
                var countOfIndex = marketStorage.CountIndex(chainId.Value);

                //checking pool
                var poolItems = marketPoolManager.GetAllActionItemByType(types);
                if (poolItems.Any())
                {
                    _logger.Information(string.Format("Some offers found in pool. Loading them too."));
                    poolItems.Reverse();

                    foreach (var itemPool in poolItems)
                    {
                        var marketData = (MarketItemV1)itemPool;

                        if (!string.IsNullOrEmpty(marketData.BuyerSignature))
                        {
                            var itemMarketBytes = marketData.ToByteArrayForSign();
                            var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, marketData.BuyerSignature);

                            foreach (var itemPubKey in itemPubKeys)
                            {
                                foreach (var itemUserPubKey in userPubKeys)
                                {
                                    if (itemPubKey.SequenceEqual(itemUserPubKey))
                                    {
                                        if (marketData.State == (int)ProductStateEnum.Sold)
                                        {
                                            _logger.Information(string.Format("Found Sold MarketItem for buyer - item hash {0}.", marketData.Hash));
                                            marketData.IsInPool = true;
                                            result.Add(marketData);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                //checking blockchain
                for (long i = (countOfIndex - 1); i >= 0; i--)
                {
                    var blockHashId = marketStorage.IndexBlockHash(chainId.Value, i);
                    var block = marketStorage.GetBlock<MarketAction>(blockHashId.Value);

                    foreach (var itemTx in block.Transactions)
                    {
                        foreach (var itemAction in itemTx.Actions)
                        {
                            foreach (var itemMarket in itemAction.BaseItems)
                            {
                                if (types.Contains(itemMarket.GetType()))
                                {
                                    var marketData = (MarketItemV1)itemMarket;

                                    if (!string.IsNullOrEmpty(marketData.BuyerSignature))
                                    {
                                        var itemMarketBytes = itemMarket.ToByteArrayForSign();
                                        var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, marketData.BuyerSignature);

                                        foreach (var itemPubKey in itemPubKeys)
                                        {
                                            foreach (var itemUserPubKey in userPubKeys)
                                            {
                                                if (itemPubKey.SequenceEqual(itemUserPubKey))
                                                {
                                                    if (marketData.State == (int)ProductStateEnum.Sold)
                                                    {
                                                        _logger.Information(string.Format("Found Sold MarketItem for buyer - item hash {0}.", itemMarket.Hash));
                                                        result.Add(marketData);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Get all active offers in market blockchain with old data filtration
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllActiveOffers(
            MarketPoolManager marketPoolManager,
            IBlockChainManager<MarketAction> marketBlockChainManager,
            MarketCategoryEnum category = MarketCategoryEnum.All)
        {
            lock (_locked)
            {
                _logger.Information(string.Format("GetAllActiveOffers {0}.", (int)category));

                var result = new List<MarketItemV1>();

                var types = new Type[] { typeof(MarketItemV1) };
                var ignoredSignatures = new List<string>();

                var marketStorage = marketBlockChainManager.Storage;
                var chainId = marketStorage.GetCanonicalChainId();
                var countOfIndex = marketStorage.CountIndex(chainId.Value);

                //checking pool
                var poolItems = marketPoolManager.GetAllActionItemByType(types);
                if (poolItems.Any())
                {
                    _logger.Information(string.Format("Some offers found in pool. Loading them too."));
                    poolItems.Reverse();

                    foreach (var itemPool in poolItems)
                    {
                        var marketData = (MarketItemV1)itemPool;

                        if ((category == MarketCategoryEnum.All)
                            && (category != MarketCategoryEnum.All) && (marketData.Category == (int)category))
                        {
                            if (!ignoredSignatures.Exists(a => a == marketData.Signature))
                            {
                                if (string.IsNullOrEmpty(marketData.BuyerSignature)
                                    && (marketData.State == (int)ProductStateEnum.Default))
                                {
                                    _logger.Information(string.Format("Found MarketItem data hash {0}.", marketData.Hash));
                                    marketData.IsInPool = true;
                                    result.Add(marketData);
                                }

                                var chainSignatures = GetChainSignaturesForMarketItem(
                                    marketStorage, chainId, countOfIndex, marketData, types);

                                ignoredSignatures.AddRange(chainSignatures);
                            }
                        }
                    }
                }

                //checking blockchain
                for (long i = (countOfIndex - 1); i >= 0; i--)
                {
                    var blockHashId = marketStorage.IndexBlockHash(chainId.Value, i);
                    var block = marketStorage.GetBlock<MarketAction>(blockHashId.Value);

                    foreach (var itemTx in block.Transactions)
                    {
                        foreach (var itemAction in itemTx.Actions)
                        {
                            foreach (var itemMarket in itemAction.BaseItems)
                            {
                                if (types.Contains(itemMarket.GetType()))
                                {
                                    var marketData = (MarketItemV1)itemMarket;

                                    if ((category == MarketCategoryEnum.All)
                                        || (category != MarketCategoryEnum.All) && (marketData.Category == (int)category))
                                    {
                                        if (!ignoredSignatures.Exists(a => a == marketData.Signature))
                                        {
                                            if (string.IsNullOrEmpty(marketData.BuyerSignature)
                                                && (marketData.State == (int)ProductStateEnum.Default))
                                            {
                                                _logger.Information(string.Format("Found MarketItem data hash {0}.", marketData.Hash));
                                                result.Add(marketData);
                                            }

                                            var chainSignatures = GetChainSignaturesForMarketItem(
                                                marketStorage, chainId, countOfIndex, marketData, types);

                                            ignoredSignatures.AddRange(chainSignatures);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Return Market Item by hash and signature
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public MarketItemV1 GetOfferBySignature(
            string signature,
            MarketPoolManager marketPoolManager,
            IBlockChainManager<MarketAction> marketBlockChainManager)
        {
            lock (_locked)
            {
                _logger.Information(string.Format("GetOfferBySignature signature {0}.", signature));

                var types = new Type[] { typeof(MarketItemV1) };

                //checking pool
                var poolItems = marketPoolManager.GetAllActionItemByType(types);
                if (poolItems.Any())
                {
                    _logger.Information(string.Format("Some offers found in pool. Checking them by signature."));
                    poolItems.Reverse();

                    foreach (var itemPool in poolItems)
                    {
                        var marketData = (MarketItemV1)itemPool;
                        if (marketData.Signature == signature)
                        {
                            marketData.IsInPool = true;
                            return marketData;
                        }
                    }
                }

                //checking blockchain
                var marketStorage = marketBlockChainManager.Storage;
                var chainId = marketStorage.GetCanonicalChainId();
                var countOfIndex = marketStorage.CountIndex(chainId.Value);

                for (long i = (countOfIndex - 1); i >= 0; i--)
                {
                    var blockHashId = marketStorage.IndexBlockHash(chainId.Value, i);
                    var block = marketStorage.GetBlock<MarketAction>(blockHashId.Value);

                    foreach (var itemTx in block.Transactions)
                    {
                        foreach (var itemAction in itemTx.Actions)
                        {
                            foreach (var itemMarket in itemAction.BaseItems)
                            {
                                if (types.Contains(itemMarket.GetType()))
                                {
                                    var marketData = (MarketItemV1)itemMarket;
                                    if (marketData.Signature == signature)
                                    {
                                        return marketData;
                                    }
                                }
                            }
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Sign market data
        /// </summary>
        /// <param name="marketData"></param>
        /// <returns></returns>
        public MarketItemV1 SignMarketData(
            MarketItemV1 marketData, UserPrivateKey privateKey)
        {
            lock (_locked)
            {
                //deep cloning so that all the refernce types are disconected, modifying ref will invalidate signature
                var clone = marketData.Clone<MarketItemV1>();

                clone.BaseSignature = clone.Signature;

                var bytesToSign = clone.ToByteArrayForSign();
                clone.Signature = Convert.ToBase64String(privateKey.Sign(bytesToSign));

                clone.Hash = clone.GenerateHash();

                return clone;
            }
        }

        /// <summary>
        /// Sign buyer market data
        /// </summary>
        /// <param name="marketData"></param>
        /// <returns></returns>
        public MarketItemV1 SignBuyerMarketData(
            MarketItemV1 marketData,
            string onionAddress,
            UserPrivateKey privateKey)
        {
            lock (_locked)
            {
                //deep cloning so that all the refernce types are disconected, modifying ref will invalidate signature
                var clone = marketData.Clone<MarketItemV1>();

                clone.State = (int)ProductStateEnum.Sold;
                clone.BuyerOnionEndpoint = onionAddress;

                var bytesToSign = clone.ToByteArrayForSign();
                clone.BuyerSignature = Convert.ToBase64String(privateKey.Sign(bytesToSign));

                clone.Hash = marketData.GenerateHash();

                return clone;
            }
        }

        /// <summary>
        /// Get chain of signatures for market item
        /// </summary>
        /// <param name="marketBlockChain"></param>
        /// <param name="chainId"></param>
        /// <param name="countOfIndex"></param>
        /// <param name="itemMarket"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        private List<string> GetChainSignaturesForMarketItem(
            DefaultStore marketBlockChain,
            Guid? chainId,
            long countOfIndex,
            MarketItemV1 itemMarket,
            Type[] types)
        {
            lock (_locked)
            {
                var result = new List<string>();
                result.Add(itemMarket.Signature);

                if (!string.IsNullOrEmpty(itemMarket.BaseSignature))
                {
                    var lookingForSignature = itemMarket.BaseSignature;

                    for (long i = (countOfIndex - 1); i >= 0; i--)
                    {
                        var blockHashId = marketBlockChain.IndexBlockHash(chainId.Value, i);
                        var block = marketBlockChain.GetBlock<MarketAction>(blockHashId.Value);

                        foreach (var itemTx in block.Transactions)
                        {
                            foreach (var itemAction in itemTx.Actions)
                            {
                                foreach (var item in itemAction.BaseItems)
                                {
                                    if (types.Contains(item.GetType()) && item.Signature == lookingForSignature)
                                    {
                                        var marketData = (MarketItemV1)item;
                                        result.Add(marketData.Signature);

                                        lookingForSignature = marketData.BaseSignature;

                                        if (string.IsNullOrEmpty(lookingForSignature))
                                        {
                                            return result;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// Get new seller market items from pool
        /// </summary>
        /// <param name="pubKey"></param>
        /// <param name="chainMarketItems"></param>
        /// <param name="userPubKey"></param>
        /// <param name="marketPoolManager"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllSellerMarketItemsByPubKeysFromPool(
            List<MarketItemV1> chainMarketItems,
            byte[] userPubKey,
            MarketPoolManager marketPoolManager)
        {
            return GetAllSellerMarketItemsByPubKeysFromPool(chainMarketItems, new List<byte[]> { userPubKey }, marketPoolManager);
        }

        /// <summary>
        /// Get new seller market items from pool
        /// </summary>
        /// <param name="chainMarketItems"></param>
        /// <param name="userPubKeys"></param>
        /// <param name="marketPoolManager"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllSellerMarketItemsByPubKeysFromPool(
            List<MarketItemV1> chainMarketItems,
            List<byte[]> userPubKeys,
            MarketPoolManager marketPoolManager)
        {
            _logger.Information(string.Format("GetAllSellerMarketItemsByPubKeysFromPool."));

            var result = new List<MarketItemV1>();
            var types = new Type[] { typeof(MarketItemV1) };
            var poolItems = marketPoolManager.GetAllActionItemByType(types);
            
            if (poolItems.Any())
            {
                _logger.Information(string.Format("Some offers found in pool. Loading them too."));
                poolItems.Reverse();

                foreach (var itemPool in poolItems)
                {
                    var marketData = (MarketItemV1)itemPool;
                    if (marketData.State == (int)ProductStateEnum.Default)
                    {
                        var itemMarketBytes = marketData.ToByteArrayForSign();
                        var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, marketData.Signature);

                        foreach (var itemPubKey in itemPubKeys)
                        {
                            foreach (var itemUserPubKey in userPubKeys)
                            {
                                if (itemPubKey.SequenceEqual(itemUserPubKey))
                                {
                                    _logger.Information(string.Format("Found Pool MarketItem for seller - item hash {0}.", marketData.Hash));
                                    marketData.IsInPool = true;
                                    result.Add(marketData);
                                }
                            }
                        }
                    }
                }

                result = RemoveChainSignaturesFromChainList(chainMarketItems, result);
            } 
            else
            {
                result = chainMarketItems;
            }

            return result;
        }

        /// <summary>
        /// Get new buyer market items from pool
        /// </summary>
        /// <param name="pubKey"></param>
        /// <param name="chainMarketItems"></param>
        /// <param name="userPubKey"></param>
        /// <param name="marketPoolManager"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllBuyerMarketItemsByPubKeysFromPool(
            List<MarketItemV1> chainMarketItems,
            byte[] userPubKey,
            MarketPoolManager marketPoolManager)
        {
            return GetAllBuyerMarketItemsByPubKeysFromPool(chainMarketItems, new List<byte[]> { userPubKey }, marketPoolManager);
        }

        /// <summary>
        /// Get new buyer market items from pool
        /// </summary>
        /// <param name="chainMarketItems"></param>
        /// <param name="userPubKeys"></param>
        /// <param name="marketPoolManager"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllBuyerMarketItemsByPubKeysFromPool(
            List<MarketItemV1> chainMarketItems,
            List<byte[]> userPubKeys,
            MarketPoolManager marketPoolManager)
        {
            _logger.Information(string.Format("GetAllSellerMarketItemsByPubKeysFromPool."));

            var result = new List<MarketItemV1>();
            var types = new Type[] { typeof(MarketItemV1) };
            var poolItems = marketPoolManager.GetAllActionItemByType(types);

            if (poolItems.Any())
            {
                _logger.Information(string.Format("Some offers found in pool. Loading them too."));
                poolItems.Reverse();

                foreach (var itemPool in poolItems)
                {
                    var marketData = (MarketItemV1)itemPool;
                    if (marketData.State == (int)ProductStateEnum.Sold)
                    {
                        var itemMarketBytes = marketData.ToByteArrayForSign();
                        var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, marketData.BuyerSignature);

                        foreach (var itemPubKey in itemPubKeys)
                        {
                            foreach (var itemUserPubKey in userPubKeys)
                            {
                                if (itemPubKey.SequenceEqual(itemUserPubKey))
                                {
                                    _logger.Information(string.Format("Found Pool MarketItem for seller - item hash {0}.", marketData.Hash));
                                    marketData.IsInPool = true;
                                    result.Add(marketData);
                                }
                            }
                        }
                    }
                }

                result = RemoveChainSignaturesFromChainList(chainMarketItems, result);
            }
            else
            {
                result = chainMarketItems;
            }

            return result;
        }

        private List<MarketItemV1> RemoveChainSignaturesFromChainList(List<MarketItemV1> chainList, List<MarketItemV1> poolList)
        {
            if (poolList.Any())
            {
                var result = poolList;
                var addIt = false;

                foreach (var itemChain in chainList)
                {
                    addIt = true;

                    foreach (var itemPool in poolList)
                    {
                        if (itemPool.BaseSignature == itemChain.Signature)
                        {
                            addIt = false;
                            break;
                        }
                    }

                    if (addIt) result.Add(itemChain);
                }

                return result;
            } 
            else
            {
                return chainList;
            }
        }
    }
}
