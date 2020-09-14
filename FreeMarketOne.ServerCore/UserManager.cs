using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.ServerCore.Helpers;
using Libplanet.Crypto;
using Libplanet.Extensions;
using LiteDB;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;

namespace FreeMarketOne.ServerCore
{
    public class UserManager
    {
        public enum PrivateKeyStates
        {
            NoKey = 0,
            NoPassword = 1,
            WrongPassword = 2,
            Valid = 3
        }

        private UserPrivateKey _privateKey;

        private IBaseConfiguration _configuration;
        private PrivateKeyStates _privateKeyState;
        private UserDataV1 _userData;
        private bool _userDataForceToPropagate;
        private ILogger _logger { get; set; }

        public PrivateKeyStates PrivateKeyState => _privateKeyState;
        public UserPrivateKey PrivateKey => _privateKey;
        public UserDataV1 UserData => _userData;
        public bool UsedDataForceToPropagate => _userDataForceToPropagate;

        public UserManager(IBaseConfiguration configuration)
        {
            _logger = Log.Logger.ForContext<ServiceManager>();
            _logger.Information("Initializing User Manager");

            _configuration = configuration;
            _privateKeyState = PrivateKeyStates.NoKey;
        }

        internal PrivateKeyStates Initialize(string password = null, UserDataV1 newUserData = null)
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
                        password = password.Substring(0, 16);
                        var key = password + password; //expecting 32 chars
                        var aes = new SymmetricKey(Encoding.ASCII.GetBytes(key));
                        var decryptedPrivKey = aes.Decrypt(keyBytes);

                        _privateKey = new UserPrivateKey(decryptedPrivKey);
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

        public string CreateRandomSeed(int length = 200)
        {
            Random random = new Random();

            var validchars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";

            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = validchars[random.Next(0, validchars.Length)];
            }

            return new string(chars);
        }

        /// <summary>
        /// Storing private key dat of user account
        /// </summary>
        /// <param name="seed">Expecting at least 200 chars.</param>
        /// <param name="password">Expecting 16 chars.</param>
        public void SaveNewPrivKey(string seed, string password, string pathRoot, string pathFileKey)
        {
            _logger.Information(string.Format("Saving new private key."));

            password = password.Substring(0, 16);
            _privateKey = new UserPrivateKey(seed);
            var key = password + password; //expecting 32 chars

            var aes = new SymmetricKey(Encoding.ASCII.GetBytes(key));
            var encryptedPrivKey = aes.Encrypt(_privateKey.ByteArray);

            var pathKey = Path.Combine(pathRoot, pathFileKey);

            var directoryPath = Path.GetDirectoryName(pathKey);
            Directory.CreateDirectory(directoryPath);

            File.WriteAllBytes(pathKey, encryptedPrivKey);
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
        public UserDataV1 GetActualUserData()
        {
            _logger.Information(string.Format("Loading user data from pool or blockchain."));

            if (_privateKey != null)
            {
                var userPubKey = _privateKey.PublicKey.KeyParam.Q.GetEncoded();
                _userData = GetUserDataByPublicKey(userPubKey);
            }

            return UserData;
        }

        /// <summary>
        /// Get public key of actual user
        /// </summary>
        /// <returns></returns>
        public byte[] GetCurrentUserPublicKey()
        {
            _logger.Information(string.Format("Getting current public key for actual user."));

            if (_privateKey != null)
            {
                return _privateKey.PublicKey.KeyParam.Q.GetEncoded();
            }

            return null;
        }

        /// <summary>
        /// Get user data by his public key
        /// </summary>
        /// <param name="userPubKeys"></param>
        /// <returns></returns>
        public UserDataV1 GetUserDataByPublicKey(byte[] userPubKey)
        {
            return GetUserDataByPublicKey(new List<byte[]> { userPubKey });
        }

        /// <summary>
        /// Get user data by his public keys
        /// </summary>
        /// <param name="userPubKeys"></param>
        /// <returns></returns>
        public UserDataV1 GetUserDataByPublicKey(List<byte[]> userPubKeys)
        {
            var types = new Type[] { typeof(UserDataV1) };

            //checking pool
            var poolItems = FreeMarketOneServer.Current.BasePoolManager.GetAllActionItemByType(types);
            if (poolItems.Any())
            {
                _logger.Information(string.Format("Some UserData found in pool. Checking if they are mine."));
                poolItems.Reverse();

                foreach (var itemPool in poolItems)
                {
                    var itemPoolBytes = itemPool.ToByteArrayForSign();
                    var itemPubKeys = UserPublicKey.Recover(itemPoolBytes, itemPool.Signature);

                    foreach (var itemPubKey in itemPubKeys)
                    {
                        foreach (var itemUserPubKey in userPubKeys)
                        {
                            if (itemPubKey.SequenceEqual(itemUserPubKey))
                            {
                                _logger.Information(string.Format("Found UserData in pool."));

                                var userData = (UserDataV1)itemPool;
                                if (!string.IsNullOrEmpty(userData.BaseSignature))
                                {
                                    if (UserManagerHelper.VerifyUserDataByBaseSignature(userData.BaseSignature, userData, itemPubKeys))
                                    {
                                        return userData;
                                    }
                                    else
                                    {
                                        _logger.Information(string.Format("UserData arent valid : {0}", userData.BaseSignature));
                                    }
                                }
                                else
                                {
                                    return userData;
                                }
                            }
                        }
                    }
                }
            }

            //checking blockchain
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
                            if (types.Contains(itemBase.GetType()))
                            {
                                var itemBaseBytes = itemBase.ToByteArrayForSign();
                                var itemPubKeys = UserPublicKey.Recover(itemBaseBytes, itemBase.Signature);

                                foreach (var itemPubKey in itemPubKeys)
                                {
                                    foreach (var itemUserPubKey in userPubKeys)
                                    {
                                        if (itemPubKey.SequenceEqual(itemUserPubKey))
                                        {
                                            _logger.Information(string.Format("Found UserData in chain."));

                                            var userData = (UserDataV1)itemBase;
                                            if (!string.IsNullOrEmpty(userData.BaseSignature))
                                            {
                                                if (UserManagerHelper.VerifyUserDataByBaseSignature(userData.BaseSignature, userData, itemPubKeys))
                                                {
                                                    return userData;
                                                } 
                                                else
                                                {
                                                    _logger.Information(string.Format("UserData arent valid : {0}.", userData.BaseSignature));
                                                }
                                            }
                                            else
                                            {
                                                return userData;
                                            }

                                            return userData;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get user data by signature and hash - signature is compared by string comparer only
        /// </summary>
        /// <param name="signature"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public UserDataV1 GetUserDataBySignatureAndHash(string signature, string hash)
        {
            var types = new Type[] { typeof(UserDataV1) };

            _logger.Information(string.Format("GetUserDataBySignatureAndHash signature {0} hash {1}.", signature, hash));

            //checking pool
            var poolItems = FreeMarketOneServer.Current.BasePoolManager.GetAllActionItemByType(types);
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

        /// <summary>
        /// Get all review based on user public key
        /// </summary>
        /// <param name="pubKey"></param>
        /// <returns></returns>
        public List<ReviewUserDataV1> GetAllReviewsForPubKey(byte[] pubKey)
        {
            return GetAllReviewsForPubKey(new List<byte[]> { pubKey });
        }

        /// <summary>
        /// Get all review based on user public keys
        /// </summary>
        /// <param name="userPubKeys"></param>
        /// <returns></returns>
        public List<ReviewUserDataV1> GetAllReviewsForPubKey(List<byte[]> userPubKeys)
        {
            _logger.Information(string.Format("GetAllReviewsForPubKey."));

            var typesReview = new Type[] { typeof(ReviewUserDataV1) };
            var typesUser = new Type[] { typeof(UserDataV1) };
            var result = new List<ReviewUserDataV1>();

            //checking blockchain
            var baseBlockChain = FreeMarketOneServer.Current.BaseBlockChainManager.Storage;
            var chainId = baseBlockChain.GetCanonicalChainId();
            var countOfIndex = baseBlockChain.CountIndex(chainId.Value);
            
            var userData = GetUserDataByPublicKey(userPubKeys);
            var allUserDatas = UserManagerHelper.GetChainUserData(
                baseBlockChain, chainId, countOfIndex, userData, typesUser);

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
                            if (typesReview.Contains(itemBase.GetType()))
                            {
                                var reviewData = (ReviewUserDataV1)itemBase;

                                foreach (var itemUserData in allUserDatas)
                                {
                                    if ((reviewData.UserSignature == itemUserData.Signature)
                                        && (reviewData.Hash == itemUserData.Hash))
                                    {
                                        _logger.Information(
                                            string.Format("Found UserData signature {0} hash {1}.", 
                                            itemUserData.Signature,
                                            itemUserData.Hash));

                                        result.Add(reviewData);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public UserDataV1 SignUserData(string userName, string description, UserDataV1 userData = null)
        {
            if (userData == null) userData = new UserDataV1();

            userData.UserName = userName;
            userData.Description = description;
            userData.BaseSignature = userData.Signature;

            var bytesToSign = userData.ToByteArrayForSign();

            userData.Signature = Convert.ToBase64String(FreeMarketOneServer.Current.UserManager.PrivateKey.Sign(bytesToSign));

            userData.Hash = userData.GenerateHash();

            return userData;
        }
    }
}
