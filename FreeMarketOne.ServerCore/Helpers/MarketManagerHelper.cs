using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.RocksDBStore;
using Libplanet.Tx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeMarketOne.ServerCore.Helpers
{
    internal static class MarketManagerHelper
    {
        /// <summary>
        /// Return whole chain of signatures to ignore old block data
        /// </summary>
        /// <param name="marketBlockChain"></param>
        /// <param name="chainId"></param>
        /// <param name="countOfIndex"></param>
        /// <param name="itemMarket"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        internal static List<string> GetChainSignaturesForMarketItem(
            RocksDBStore marketBlockChain,
            Guid? chainId,
            long countOfIndex, 
            MarketItemV1 itemMarket,
            Type[] types)
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
