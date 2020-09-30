using FreeMarketOne.BlockChain;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Pools;
using FreeMarketOne.Users;
using Libplanet.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Users
{
    public interface IUserManager
    {
        bool UsedDataForceToPropagate { get; }
        UserPrivateKey PrivateKey { get; set; }
        public UserManager.PrivateKeyStates PrivateKeyState { get; }
        public UserDataV1 UserData { get; }
        UserManager.PrivateKeyStates Initialize(string password = null, UserDataV1 newUserData = null);
        string CreateRandomSeed(int length = 200);
        void SaveNewPrivKey(string seed, string password, string pathRoot, string pathFileKey);
        void SaveUserData(UserDataV1 userData, string pathRoot, string pathFileUserData);
        UserDataV1 GetActualUserData(BasePoolManager basePoolManager, IBlockChainManager<BaseAction> baseBlockChainManager);
        byte[] GetCurrentUserPublicKey();
        UserDataV1 GetUserDataBySignatureAndHash(
            string signature,
            string hash,
            BasePoolManager basePoolManager,
            IBlockChainManager<BaseAction> blockChainManager);
        List<ReviewUserDataV1> GetAllReviewsForPubKey(
            byte[] pubKey,
            BasePoolManager basePoolManager,
            IBlockChainManager<BaseAction> baseBlockChain);
        List<ReviewUserDataV1> GetAllReviewsForPubKey(
            List<byte[]> userPubKeys,
            BasePoolManager basePoolManager,
            IBlockChainManager<BaseAction> baseBlockChain);
        double GetUserReviewStars(List<ReviewUserDataV1> userReviews);
        UserDataV1 SignUserData(string userName, string description, UserDataV1 userData = null);
        UserDataV1 GetUserDataByPublicKey(
            byte[] userPubKey,
            BasePoolManager basePoolManager,
            IBlockChainManager<BaseAction> blockChainManager);
        UserDataV1 GetUserDataByPublicKey(
            List<byte[]> userPubKeys,
            BasePoolManager basePoolManager,
            IBlockChainManager<BaseAction> blockChainManager);
    }
}
