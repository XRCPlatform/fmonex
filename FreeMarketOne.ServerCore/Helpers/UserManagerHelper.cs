using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.Extensions;
using Libplanet.RocksDBStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeMarketOne.ServerCore.Helpers
{
    internal static class UserManagerHelper
    {
        /// <summary>
        /// Verify that updated chain data are equal to base pub key
        /// </summary>
        /// <param name="baseSignature"></param>
        /// <param name="userData"></param>
        /// <param name="userPubKeys"></param>
        /// <returns></returns>
        internal static bool VerifyUserDataByBaseSignature(string baseSignature, UserDataV1 userData, List<byte[]> userPubKeys)
        {
            var types = new Type[] { typeof(UserDataV1) };
            var baseBlockChain = FreeMarketOneServer.Current.BaseBlockChainManager.Storage;
            var chainId = baseBlockChain.GetCanonicalChainId();
            var countOfIndex = baseBlockChain.CountIndex(chainId.Value);

            for (long i = (countOfIndex - 1); i >= 0; i--)
            {
                var blockHashId = baseBlockChain.IndexBlockHash(chainId.Value, i);
                var block = baseBlockChain.GetBlock<BaseAction>(blockHashId.Value);

                foreach (var itemTx in block.Transactions)
                {
                    foreach (var itemAction in itemTx.Actions)
                    {
                        foreach (var itemBase in itemAction.BaseItems)
                        {
                            if (types.Contains(itemBase.GetType()) && (itemBase.Signature == baseSignature))
                            {
                                var itemBaseBytes = itemBase.ToByteArrayForSign();
                                var itemPubKeys = UserPublicKey.Recover(itemBaseBytes, itemBase.Signature);

                                foreach (var itemPubKey in itemPubKeys)
                                {
                                    foreach (var itemUserPubKey in userPubKeys)
                                    {
                                        if (itemPubKey.SequenceEqual(itemUserPubKey))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Return whole chain of signatures to ignore old block data
        /// </summary>
        /// <param name="marketBlockChain"></param>
        /// <param name="chainId"></param>
        /// <param name="countOfIndex"></param>
        /// <param name="itemMarket"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        internal static List<UserDataV1> GetChainUserData(
            RocksDBStore baseBlockChain, 
            Guid? chainId, 
            long countOfIndex, 
            UserDataV1 userItem,
            Type[] types)
        {
            var result = new List<UserDataV1>();
            result.Add(userItem);

            if (!string.IsNullOrEmpty(userItem.BaseSignature))
            {
                var lookingForSignature = userItem.BaseSignature;

                for (long i = (countOfIndex - 1); i >= 0; i--)
                {
                    var blockHashId = baseBlockChain.IndexBlockHash(chainId.Value, i);
                    var block = baseBlockChain.GetBlock<BaseAction>(blockHashId.Value);

                    foreach (var itemTx in block.Transactions)
                    {
                        foreach (var itemAction in itemTx.Actions)
                        {
                            foreach (var item in itemAction.BaseItems)
                            {
                                if (types.Contains(item.GetType()) && item.Signature == lookingForSignature)
                                {
                                    var userData = (UserDataV1)item;
                                    result.Add(userData);
                                    lookingForSignature = userData.BaseSignature;

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
