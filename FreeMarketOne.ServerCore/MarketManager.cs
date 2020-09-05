using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
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
        /// Get all seller market items with all "revisions"
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetSellerMarketItemsByPubKeys(byte[] pubKey)
        {
            return GetSellerMarketItemsByPubKeys(new List<byte[]> { pubKey });
        }

        /// <summary>
        /// Get all seller market items with all "revisions"
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetSellerMarketItemsByPubKeys(List<byte[]> userPubKeys)
        {
            var result = new List<MarketItemV1>();

            var types = new Type[] { typeof(MarketItemV1) };

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
                                var itemMarketBytes = itemMarket.ToByteArrayForSign();
                                var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, itemMarket.Signature);

                                foreach (var itemPubKey in itemPubKeys)
                                {
                                    foreach (var itemUserPubKey in userPubKeys)
                                    {
                                        if (itemPubKey.SequenceEqual(itemUserPubKey))
                                        {
                                            _logger.Information(string.Format("Found MarketItem for seller - item hash {0}.", itemMarket.Hash));

                                            result.Add((MarketItemV1)itemMarket);
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
        /// Get all buyer market items with all "revisions"
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetBuyerMarketItemsByPubKeys(byte[] pubKey)
        {
            return GetBuyerMarketItemsByPubKeys(new List<byte[]> { pubKey });
        }

        /// <summary>
        /// Get all buyer market items with all "revisions"
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<MarketItemV1> GetBuyerMarketItemsByPubKeys(List<byte[]> userPubKeys)
        {
            var result = new List<MarketItemV1>();

            var types = new Type[] { typeof(MarketItemV1) };

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

                                if (!string.IsNullOrEmpty(marketData.BuyerSignature)) {

                                    var itemMarketBytes = itemMarket.ToByteArrayForSign();
                                    var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, marketData.BuyerSignature);

                                    foreach (var itemPubKey in itemPubKeys)
                                    {
                                        foreach (var itemUserPubKey in userPubKeys)
                                        {
                                            if (itemPubKey.SequenceEqual(itemUserPubKey))
                                            {
                                                _logger.Information(string.Format("Found MarketItem for buyer - item hash {0}.", itemMarket.Hash));

                                                result.Add((MarketItemV1)itemMarket);
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
}
