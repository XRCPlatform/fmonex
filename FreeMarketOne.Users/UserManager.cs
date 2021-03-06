using FreeMarketOne.BlockChain;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Common;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Pools;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Libplanet.Store;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;

namespace FreeMarketOne.Users
{
    public class UserManager : IUserManager
    {
        public enum PrivateKeyStates
        {
            NoKey = 0,
            NoPassword = 1,
            WrongPassword = 2,
            Valid = 3
        }

        public UserPrivateKey PrivateKey { get; set; }

        private IBaseConfiguration _configuration;
        private PrivateKeyStates _privateKeyState;
        private UserDataV1 _userData;
        private bool _userDataForceToPropagate;
        private ILogger _logger { get; set; }

        public PrivateKeyStates PrivateKeyState => _privateKeyState;
        public UserDataV1 UserData => _userData;
        public bool UsedDataForceToPropagate => _userDataForceToPropagate;
        private SHAProcessor _shaProcessor;

        private readonly object _locked = new object();
        private readonly MemoryCache _cache;
        private readonly CacheItemPolicy _cachePolicy;

        public UserManager(IBaseConfiguration configuration)
        {
            _logger = Log.Logger.ForContext<UserManager>();
            _logger.Information("Initializing User Manager");

            _configuration = configuration;
            _privateKeyState = PrivateKeyStates.NoKey;
            _shaProcessor = new SHAProcessor();
            _cache = new MemoryCache("user.pubkey");
            _cachePolicy = new CacheItemPolicy();
            _cachePolicy.SlidingExpiration = TimeSpan.FromMinutes(120);
        }

        /// <summary>
        /// Inicialization of user manager class
        /// </summary>
        /// <param name="password"></param>
        /// <param name="newUserData"></param>
        /// <returns></returns>
        public PrivateKeyStates Initialize(string password = null, UserDataV1 newUserData = null)
        {
            var fileKeyPath = Path.Combine(_configuration.FullBaseDirectory, _configuration.BlockChainSecretPath);
            var fileUserPath = Path.Combine(_configuration.FullBaseDirectory, _configuration.BlockChainUserPath);

            if (File.Exists(fileKeyPath))
            {
                try
                {
                    var keyBytes = File.ReadAllBytes(fileKeyPath);

                    if (password != null)
                    {
                        string key = GenerateKey(password);
                        var aes = new SymmetricKey(Encoding.ASCII.GetBytes(key));
                        var decryptedPrivKey = aes.Decrypt(keyBytes);

                        PrivateKey = new UserPrivateKey(decryptedPrivKey);
                        _privateKeyState = PrivateKeyStates.Valid;

                        _logger.Information(string.Format("Private Key Decrypted."));

                        if (newUserData != null)
                        {
                            _userDataForceToPropagate = true;
                            _userData = newUserData;
                        }
                        else
                        {
                            if (File.Exists(fileUserPath))
                            {
                                var userBytes = File.ReadAllBytes(fileUserPath);
                                var serializedItems = ZipHelper.Decompress(userBytes);
                                _userData = JsonConvert.DeserializeObject<UserDataV1>(serializedItems);
                            }
                        }
                    }
                    else
                    {
                        _privateKeyState = PrivateKeyStates.NoPassword;
                    }
                }
                catch (InvalidCiphertextException)
                {
                    _privateKeyState = PrivateKeyStates.WrongPassword;
                }
                catch (Exception)
                {
                    _privateKeyState = PrivateKeyStates.NoKey;
                }
            }
            else
            {
                _privateKeyState = PrivateKeyStates.NoKey;
            }

            return _privateKeyState;
        }

        /// <summary>
        /// Create random seed for user
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public string CreateRandomSeed(int length = 200)
        {
            Random random = new Random();

            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = TextHelper.CHARS[random.Next(0, TextHelper.CHARS.Length)];
            }

            return new string(chars);
        }

        /// <summary>
        /// Storing private key dat of user account
        /// </summary>
        /// <param name="seed">Expecting at least 200 chars.</param>
        /// <param name="password">Expecting 16-32 chars.</param>
        public void SaveNewPrivKey(string seed, string password, string pathRoot, string pathFileKey)
        {
            _logger.Information(string.Format("Saving new private key."));
            
            PrivateKey = new UserPrivateKey(seed);
            string key = GenerateKey(password);
            var aes = new SymmetricKey(Encoding.ASCII.GetBytes(key));
            var encryptedPrivKey = aes.Encrypt(PrivateKey.ByteArray);

            var pathKey = Path.Combine(pathRoot, pathFileKey);

            var directoryPath = Path.GetDirectoryName(pathKey);
            Directory.CreateDirectory(directoryPath);

            File.WriteAllBytes(pathKey, encryptedPrivKey);
        }

        public string GenerateKey(string password)
        {
            int chopIndex = 32;
            if (password.Length < chopIndex)
            {
                chopIndex = password.Length;
            }
            password = password.Substring(0, chopIndex);
            string shaHash = _shaProcessor.GetSHA256(password);
            char[] blender = shaHash.ToCharArray();
            for (int i = 0; i < password.Length; i++)
            {
                blender[i] = password[i];
            }
            return new string(blender).Substring(0, 32); 
            
        }

        /// <summary>
        /// Store user data to cache for quick usage
        /// </summary>
        /// <param name="userData"></param>
        /// <param name="pathRoot"></param>
        /// <param name="pathUserData"></param>
        public void SaveUserData(UserDataV1 userData, string pathRoot, string pathFileUserData)
        {
            _logger.Information(string.Format("Saving new user data."));

            var serializedUserData = JsonConvert.SerializeObject(userData);
            var compressedUserData = ZipHelper.Compress(serializedUserData);

            var pathUserData = Path.Combine(pathRoot, pathFileUserData);

            var directoryPath = Path.GetDirectoryName(pathUserData);
            Directory.CreateDirectory(directoryPath);

            File.WriteAllBytes(pathUserData, compressedUserData);
        }

        /// <summary>
        /// Get actual user data from blockchain or pool
        /// </summary>
        /// <returns></returns>
        public UserDataV1 GetActualUserData(BasePoolManager basePoolManager, IBlockChainManager<BaseAction> baseBlockChainManager)
        {
            _logger.Information(string.Format("Loading user data from pool or blockchain."));

            if (PrivateKey != null)
            {
                var userPubKey = PrivateKey.PublicKey.KeyParam.Q.GetEncoded();
                _userData = GetUserDataByPublicKey(userPubKey, basePoolManager, baseBlockChainManager);
            }

            return _userData;
        }

        /// <summary>
        /// Get public key of actual user
        /// </summary>
        /// <returns></returns>
        public byte[] GetCurrentUserPublicKey()
        {
            _logger.Information(string.Format("Getting current public key for actual user."));

            if (PrivateKey != null)
            {
                return PrivateKey.PublicKey.KeyParam.Q.GetEncoded();
            }

            return null;
        }

        /// <summary>
        /// Get user data by his public key
        /// </summary>
        /// <param name="userPubKeys"></param>
        /// <returns></returns>
        public UserDataV1 GetUserDataByPublicKey(
            byte[] userPubKey, 
            BasePoolManager basePoolManager,
            IBlockChainManager<BaseAction> blockChainManager)
        {
            return GetUserDataByPublicKey(new List<byte[]> { userPubKey }, basePoolManager, blockChainManager);
        }

        /// <summary>
        /// Get user data by his public keys
        /// </summary>
        /// <param name="userPubKeys"></param>
        /// <returns></returns>
        public UserDataV1 GetUserDataByPublicKey(
            List<byte[]> userPubKeys, 
            BasePoolManager basePoolManager,
            IBlockChainManager<BaseAction> blockChainManager)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            lock (_locked)
            {
                foreach (var item in userPubKeys)
                {
                    var publicKey = Convert.ToBase64String(item);
                    UserDataV1 retval = _cache.Get(publicKey) as UserDataV1;
                    if (retval != null)
                    {
                        _logger.Information($"UserData found in cache. elapsed {sw.ElapsedMilliseconds}");
                        return retval;
                    }
                }
                

                var types = new Type[] { typeof(UserDataV1) };

                //checking pool
                var poolItems = basePoolManager.GetAllActionItemByType(types);
                if (poolItems.Any())
                {
                    _logger.Information($"Some UserData found in pool. Checking if they are mine. elapsed {sw.ElapsedMilliseconds}");
                    poolItems.Reverse();

                    foreach (var itemPool in poolItems)
                    {
                        var userData = (UserDataV1)itemPool;
                        if (userData.PublicKey != null)
                        {
                            _cache.Set(userData.PublicKey, userData, _cachePolicy);
                            var publicKeyBytes = Convert.FromBase64String(userData.PublicKey);

                            foreach (var itemUserPubKey in userPubKeys)
                            {
                                if (publicKeyBytes.SequenceEqual(itemUserPubKey))
                                {
                                    _logger.Information($"Found UserData in pool. elapsed {sw.ElapsedMilliseconds}");

                                    return userData;
                                }
                            }
                        }
                    }
                }

                //checking blockchain
                var baseStorage = blockChainManager.Storage;
                var chainId = baseStorage.GetCanonicalChainId();
                var countOfIndex = baseStorage.CountIndex(chainId.Value);

                for (long i = (countOfIndex - 1); i >= 0; i--)
                {
                    var blockHashId = baseStorage.IndexBlockHash(chainId.Value, i);
                    var block = baseStorage.GetBlock<BaseAction>(blockHashId.Value);

                    foreach (var itemTx in block.Transactions)
                    {
                        foreach (var itemAction in itemTx.Actions)
                        {
                            foreach (var itemBase in itemAction.BaseItems)
                            {
                                if (types.Contains(itemBase.GetType()))
                                {
                                    var userData = (UserDataV1)itemBase;
                                    if (userData.PublicKey != null)
                                    {
                                        _cache.Set(userData.PublicKey, userData, _cachePolicy);
                                        var publicKeyBytes = Convert.FromBase64String(userData.PublicKey);

                                        foreach (var itemUserPubKey in userPubKeys)
                                        {
                                            if (publicKeyBytes.SequenceEqual(itemUserPubKey))
                                            {
                                                _logger.Information($"Found UserData in chain. elapsed {sw.ElapsedMilliseconds}");

                                                return userData;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                _logger.Information($"UserData not found on chain or in cache. elapsed {sw.ElapsedMilliseconds}");
                return null;
            }
        }

        /// <summary>
        /// Get user data by signature and hash - signature is compared by string comparer only
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public UserDataV1 GetUserDataBySignatureAndHash(
            string signature, 
            string hash,
            BasePoolManager basePoolManager,
            IBlockChainManager<BaseAction> blockChainManager)
        {
            lock (_locked)
            {
                var types = new Type[] { typeof(UserDataV1) };

                _logger.Information(string.Format("GetUserDataBySignatureAndHash signature {0} hash {1}.", signature, hash));

                //checking pool
                var poolItems = basePoolManager.GetAllActionItemByType(types);
                if (poolItems.Any())
                {
                    _logger.Information(string.Format("Some UserData found in pool."));
                    poolItems.Reverse();

                    foreach (var itemPool in poolItems)
                    {
                        if ((itemPool.Hash == hash) && (itemPool.Signature == signature))
                        {
                            return (UserDataV1)itemPool;
                        }
                    }
                }

                //checking blockchain
                var baseStorage = blockChainManager.Storage;
                var chainId = baseStorage.GetCanonicalChainId();
                var countOfIndex = baseStorage.CountIndex(chainId.Value);

                for (long i = (countOfIndex - 1); i >= 0; i--)
                {
                    var blockHashId = baseStorage.IndexBlockHash(chainId.Value, i);
                    var block = baseStorage.GetBlock<BaseAction>(blockHashId.Value);

                    foreach (var itemTx in block.Transactions)
                    {
                        foreach (var itemAction in itemTx.Actions)
                        {
                            foreach (var itemBase in itemAction.BaseItems)
                            {
                                if (types.Contains(itemBase.GetType()))
                                {
                                    if ((itemBase.Hash == hash) && (itemBase.Signature == signature))
                                    {
                                        _logger.Information(string.Format("Found UserData signature {0} hash {1}.", signature, hash));

                                        return (UserDataV1)itemBase;
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
        /// Get all review based on user public key
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<ReviewUserDataV1> GetAllReviewsForPubKey(
            byte[] pubKey,
            BasePoolManager basePoolManager,
            IBlockChainManager<BaseAction> baseBlockChain)
        {
            return GetAllReviewsForPubKey(new List<byte[]> { pubKey }, basePoolManager, baseBlockChain);
        }

        /// <summary>
        /// Get all review based on user public keys
        /// </summary>
        /// <param name="userPubKeys"></param>
        /// <returns></returns>
        public List<ReviewUserDataV1> GetAllReviewsForPubKey(
            List<byte[]> userPubKeys,
            BasePoolManager basePoolManager,
            IBlockChainManager<BaseAction> baseBlockChain)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            lock (_locked)
            {
                var typesReview = new Type[] { typeof(ReviewUserDataV1) };
                var typesUser = new Type[] { typeof(UserDataV1) };
                var result = new List<ReviewUserDataV1>();

                //checking blockchain
                var baseStorage = baseBlockChain.Storage;
                var chainId = baseStorage.GetCanonicalChainId();
                var countOfIndex = baseStorage.CountIndex(chainId.Value);

                var userData = GetUserDataByPublicKey(
                    userPubKeys,
                    basePoolManager,
                    baseBlockChain);
                
                if (userData == null)
                {
                    _logger.Information($"Reviews not found as recieved null UserData. elapsed: {sw.ElapsedMilliseconds}");
                    return result;
                }

                var allUserDatas = GetChainUserData(
                    baseStorage, chainId, countOfIndex, userData, typesUser);

                for (long i = (countOfIndex - 1); i >= 0; i--)
                {
                    var blockHashId = baseStorage.IndexBlockHash(chainId.Value, i);
                    var block = baseStorage.GetBlock<BaseAction>(blockHashId.Value);

                    foreach (var itemTx in block.Transactions)
                    {
                        foreach (var itemAction in itemTx.Actions)
                        {
                            foreach (var itemBase in itemAction.BaseItems)
                            {
                                if (typesReview.Contains(itemBase.GetType()))
                                {
                                    var reviewData = (ReviewUserDataV1)itemBase;

                                    foreach (var itemUserData in allUserDatas)
                                    {
                                        if ((reviewData.UserSignature == itemUserData.Signature)
                                            && (reviewData.UserHash == itemUserData.Hash))
                                        {
                                            _logger.Information($"Found UserData signature {itemUserData.Signature} hash {itemUserData.Hash}. elapsed: {sw.ElapsedMilliseconds}");

                                            result.Add(reviewData);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                sw.Stop();
                _logger.Information($"GetAllReviewsForPubKey. elapsed: {sw.ElapsedMilliseconds}");
                return result;
            }
        }

        /// <summary>
        /// Calculate stars based average calculation
        /// </summary>
        /// <param name="userReviews"></param>
        /// <returns></returns>
        public double GetUserReviewStars(List<ReviewUserDataV1> userReviews)
        {
            if (userReviews !=null && userReviews.Any())
            {
                var listOfStars = userReviews.Select(a => (double)a.Stars);
                var averageOfStars = listOfStars.Average();

                return averageOfStars;
            } 
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Sign user data
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="description"></param>
        /// <param name="userData"></param>
        /// <returns></returns>
        public UserDataV1 SignUserData(string userName, string description, UserDataV1 userData = null)
        {
            lock (_locked)
            {
                if (userData == null) userData = new UserDataV1();
                var clone = userData.Clone<UserDataV1>();
                clone.UserName = userName;
                clone.Description = description;
                clone.BaseSignature = clone.Signature;
                clone.PublicKey = Convert.ToBase64String(PrivateKey.PublicKey.KeyParam.Q.GetEncoded());

                var bytesToSign = clone.ToByteArrayForSign();

                clone.Signature = Convert.ToBase64String(PrivateKey.Sign(bytesToSign));

                clone.Hash = clone.GenerateHash();

                return clone;
            }
        }

        /// <summary>
        /// Get median of params
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sourceArray"></param>
        /// <param name="cloneArray"></param>
        /// <returns></returns>
        private T GetMedian<T>(T[] sourceArray, bool cloneArray = true) where T : IComparable<T>
        {
            lock (_locked)
            {
                if (sourceArray == null || sourceArray.Length == 0)
                    throw new ArgumentException("Median of empty array not defined.");

                T[] sortedArray = cloneArray ? (T[])sourceArray.Clone() : sourceArray;
                Array.Sort(sortedArray);

                //get the median
                int size = sortedArray.Length;
                int mid = size / 2;
                if (size % 2 != 0)
                    return sortedArray[mid];

                dynamic value1 = sortedArray[mid];
                dynamic value2 = sortedArray[mid - 1];
                return (sortedArray[mid] + value2) * 0.5;
            }
        }

        /// <summary>
        /// Verification of userdata with base signature
        /// </summary>
        /// <param name="baseSignature"></param>
        /// <param name="userData"></param>
        /// <param name="userPubKeys"></param>
        /// <param name="baseBlockChain"></param>
        /// <returns></returns>
        private bool VerifyUserDataByBaseSignature(
            string baseSignature,
            UserDataV1 userData,
            List<byte[]> userPubKeys,
            IBlockChainManager<BaseAction> baseBlockChain)
        {
            lock (_locked)
            {
                var types = new Type[] { typeof(UserDataV1) };
                var baseStorage = baseBlockChain.Storage;
                var chainId = baseStorage.GetCanonicalChainId();
                var countOfIndex = baseStorage.CountIndex(chainId.Value);

                for (long i = (countOfIndex - 1); i >= 0; i--)
                {
                    var blockHashId = baseStorage.IndexBlockHash(chainId.Value, i);
                    var block = baseStorage.GetBlock<BaseAction>(blockHashId.Value);

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
        private List<UserDataV1> GetChainUserData(
            DefaultStore baseBlockChain,
            Guid? chainId,
            long countOfIndex,
            UserDataV1 userItem,
            Type[] types)
        {
            lock (_locked)
            {
                var result = new List<UserDataV1>();
                result.Add(userItem);

                if (userItem != null && !string.IsNullOrEmpty(userItem.BaseSignature))
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

        public ReviewUserDataV1 SignReviewData(ReviewUserDataV1 review, UserPrivateKey privateKey)
        {
            lock (_locked)
            {
                if (review == null) review = new ReviewUserDataV1();
                //review.PublicKey = Convert.ToBase64String(PrivateKey.PublicKey.KeyParam.Q.GetEncoded());
                var clone = review.Clone<ReviewUserDataV1>();
                var bytesToSign = clone.ToByteArrayForSign();
                clone.Signature = Convert.ToBase64String(PrivateKey.Sign(bytesToSign));
                clone.Hash = clone.GenerateHash();
                return clone;
            }
        }
    }
}
