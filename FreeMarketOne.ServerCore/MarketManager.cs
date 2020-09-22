using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.ServerCore.Helpers;
using Libplanet.Extensions;
using Libplanet.RocksDBStore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeMarketOne.ServerCore
{
    public class MarketManager
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

        public MarketManager(IBaseConfiguration configuration)
        {
            _logger = Log.Logger.ForContext<ServiceManager>();
            _logger.Information("Initializing Market Manager");

            _configuration = configuration;
        }

        /// <summary>
        /// Get public key of buyer from market item
        /// </summary>
        /// <param name="itemMarket"></param>
        /// <returns></returns>
        public List<byte[]> GetBuyerPubKeyFromMarketItem(MarketItem itemMarket)
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
        public List<byte[]> GetSellerPubKeyFromMarketItem(MarketItem itemMarket)
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
        public List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(byte[] pubKey)
        {
            return GetAllSellerMarketItemsByPubKeys(new List<byte[]> { pubKey });
        }

        /// <summary>
        /// Get all seller market items with old data filtration
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllSellerMarketItemsByPubKeys(List<byte[]> userPubKeys)
        {
            lock (_locked)
            {
                _logger.Information(string.Format("GetAllSellerMarketItemsByPubKeys."));

                var result = new List<MarketItemV1>();
                var types = new Type[] { typeof(MarketItemV1) };

                var ignoredSignatures = new List<string>();

                var marketBlockChain = FreeMarketOneServer.Current.MarketBlockChainManager.Storage;
                var chainId = marketBlockChain.GetCanonicalChainId();
                var countOfIndex = marketBlockChain.CountIndex(chainId.Value);

                //checking pool
                var poolItems = FreeMarketOneServer.Current.MarketPoolManager.GetAllActionItemByType(types);
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
                                                marketBlockChain, chainId, countOfIndex, marketData, types);

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
                    var blockHashId = marketBlockChain.IndexBlockHash(chainId.Value, i);
                    var block = marketBlockChain.GetBlock<MarketAction>(blockHashId.Value);

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
                                                        result.Add(marketData);
                                                    }
                                                    else
                                                    {
                                                        _logger.Information(string.Format("Found deleted MarketItem for seller - item hash {0}.", itemMarket.Hash));
                                                    }

                                                    var chainSignatures = GetChainSignaturesForMarketItem(
                                                            marketBlockChain, chainId, countOfIndex, marketData, types);

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
        public List<MarketItemV1> GetAllBuyerMarketItemsByPubKeys(byte[] pubKey)
        {
            return GetAllBuyerMarketItemsByPubKeys(new List<byte[]> { pubKey });
        }

        /// <summary>
        /// Get all buyer market items with old data filtration
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllBuyerMarketItemsByPubKeys(List<byte[]> userPubKeys)
        {
            lock (_locked)
            {
                _logger.Information(string.Format("GetAllBuyerMarketItemsByPubKeys."));

                var result = new List<MarketItemV1>();
                var types = new Type[] { typeof(MarketItemV1) };

                var marketBlockChain = FreeMarketOneServer.Current.MarketBlockChainManager.Storage;
                var chainId = marketBlockChain.GetCanonicalChainId();
                var countOfIndex = marketBlockChain.CountIndex(chainId.Value);

                //checking pool
                var poolItems = FreeMarketOneServer.Current.MarketPoolManager.GetAllActionItemByType(types);
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
                    var blockHashId = marketBlockChain.IndexBlockHash(chainId.Value, i);
                    var block = marketBlockChain.GetBlock<MarketAction>(blockHashId.Value);

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
        public List<MarketItemV1> GetAllActiveOffers(MarketCategoryEnum category = MarketCategoryEnum.All)
        {
            lock (_locked)
            {
                _logger.Information(string.Format("GetAllActiveOffers {0}.", (int)category));

                var result = new List<MarketItemV1>();

                var types = new Type[] { typeof(MarketItemV1) };
                var ignoredSignatures = new List<string>();

                var marketBlockChain = FreeMarketOneServer.Current.MarketBlockChainManager.Storage;
                var chainId = marketBlockChain.GetCanonicalChainId();
                var countOfIndex = marketBlockChain.CountIndex(chainId.Value);

                //checking pool
                var poolItems = FreeMarketOneServer.Current.MarketPoolManager.GetAllActionItemByType(types);
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
                                    marketBlockChain, chainId, countOfIndex, marketData, types);

                                ignoredSignatures.AddRange(chainSignatures);
                            }
                        }
                    }
                }

                //checking blockchain
                for (long i = (countOfIndex - 1); i >= 0; i--)
                {
                    var blockHashId = marketBlockChain.IndexBlockHash(chainId.Value, i);
                    var block = marketBlockChain.GetBlock<MarketAction>(blockHashId.Value);

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
                                                marketBlockChain, chainId, countOfIndex, marketData, types);

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
        public MarketItemV1 GetOfferBySignature(string signature)
        {
            lock (_locked)
            {
                _logger.Information(string.Format("GetOfferBySignature signature {0}.", signature));

                var types = new Type[] { typeof(MarketItemV1) };

                //checking pool
                var poolItems = FreeMarketOneServer.Current.MarketPoolManager.GetAllActionItemByType(types);
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
                var marketBlockChain = FreeMarketOneServer.Current.MarketBlockChainManager.Storage;
                var chainId = marketBlockChain.GetCanonicalChainId();
                var countOfIndex = marketBlockChain.CountIndex(chainId.Value);

                for (long i = (countOfIndex - 1); i >= 0; i--)
                {
                    var blockHashId = marketBlockChain.IndexBlockHash(chainId.Value, i);
                    var block = marketBlockChain.GetBlock<MarketAction>(blockHashId.Value);

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

        public MarketItemV1 SignMarketData(MarketItemV1 marketData)
        {
            lock (_locked)
            {
                marketData.BaseSignature = marketData.Signature;

                var bytesToSign = marketData.ToByteArrayForSign();
                marketData.Signature = Convert.ToBase64String(FreeMarketOneServer.Current.UserManager.PrivateKey.Sign(bytesToSign));

                marketData.Hash = marketData.GenerateHash();

                return marketData;
            }
        }

        public MarketItemV1 SignBuyerMarketData(MarketItemV1 marketData)
        {
            lock (_locked)
            {
                var publicIp = FreeMarketOneServer.Current.ServerOnionAddress.PublicIp;

                marketData.State = (int)MarketManager.ProductStateEnum.Sold;
                marketData.BuyerOnionEndpoint = publicIp.MapToIPv4().ToString();

                var bytesToSign = marketData.ToByteArrayForSign();
                marketData.BuyerSignature = Convert.ToBase64String(FreeMarketOneServer.Current.UserManager.PrivateKey.Sign(bytesToSign));

                marketData.Hash = marketData.GenerateHash();

                return marketData;
            }
        }

        private List<string> GetChainSignaturesForMarketItem(
            RocksDBStore marketBlockChain,
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
    }
}
