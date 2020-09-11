﻿using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore.Helpers;
using Libplanet.Extensions;
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
            New = 0,
            Updated = 1,
            Sold = 2,
            Deleted = 3
        }

        private IBaseConfiguration _configuration;

        private ILogger _logger { get; set; }

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
            _logger.Information(string.Format("GetAllSellerMarketItemsByPubKeys."));

            var result = new List<MarketItemV1>();
            var types = new Type[] { typeof(MarketItemV1) };

            var ignoredSignatures = new List<string>();

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
                                                _logger.Information(string.Format("Found MarketItem for seller - item hash {0}.", itemMarket.Hash));

                                                var chainSignatures = MarketManagerHelper.GetChainSignaturesForMarketItem(
                                                        marketBlockChain, chainId, countOfIndex, marketData, types);

                                                ignoredSignatures.AddRange(chainSignatures);

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

            return result;
        }

        /// <summary>
        /// Get all active offers in market blockchain with old data filtration
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetAllActiveOffers(MarketCategoryEnum category = MarketCategoryEnum.All)
        {
            _logger.Information(string.Format("GetAllActiveOffers {0}.", (int)category));

            var result = new List<MarketItemV1>();
            var types = new Type[] { typeof(MarketItemV1) };

            //checking blockchain
            var marketBlockChain = FreeMarketOneServer.Current.MarketBlockChainManager.Storage;
            var chainId = marketBlockChain.GetCanonicalChainId();
            var countOfIndex = marketBlockChain.CountIndex(chainId.Value);

            for (long i = (countOfIndex - 1); i >= 0; i--)
            {
                var blockHashId = marketBlockChain.IndexBlockHash(chainId.Value, i);
                var block = marketBlockChain.GetBlock<MarketAction>(blockHashId.Value);

                var ignoredSignatures = new List<string>();

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
                                    && (category != MarketCategoryEnum.All) && (marketData.Category == (int)category))
                                {
                                    if (!ignoredSignatures.Exists(a => a == marketData.Signature))
                                    {
                                        if (string.IsNullOrEmpty(marketData.BuyerSignature))
                                        {
                                            _logger.Information(string.Format("Found MarketItem data hash {0}.", marketData.Hash));
                                            result.Add(marketData);
                                        }

                                        var chainSignatures = MarketManagerHelper.GetChainSignaturesForMarketItem(
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

        /// <summary>
        /// Return Market Item by hash and signature
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public MarketItemV1 GetOfferBySignature(string signature)
        {
            _logger.Information(string.Format("GetOfferBySignature signature {0}.", signature));

            var types = new Type[] { typeof(MarketItemV1) };

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
}
