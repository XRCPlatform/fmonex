using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using Libplanet.Crypto;
using Libplanet.Extensions;
using LiteDB;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Linq;
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


        private const string VALIDCHARS = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";

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

                        _logger.Information(string.Format("Private Key Decrypted"));

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

            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = VALIDCHARS[random.Next(0, VALIDCHARS.Length)];
            }

            return new string(chars);
        }

        public bool IsTextValid(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    var charTest = text.Substring(i, 1);
                    if (!VALIDCHARS.Contains(charTest)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Storing private key dat of user account
        /// </summary>
        /// <param name="seed">Expecting at least 200 chars.</param>
        /// <param name="password">Expecting 16 chars.</param>
        public void SaveNewPrivKey(string seed, string password, string pathRoot, string pathFileKey) 
        {  
            _logger.Information(string.Format("Saving new private key"));

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
            _logger.Information(string.Format("Saving new user data"));

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
                var types = new Type[] { typeof(UserDataV1) };
                var userPubKey = _privateKey.PublicKey.KeyParam.Q.GetEncoded();

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
                            if (itemPubKey.SequenceEqual(userPubKey))
                            {
                                _logger.Information(string.Format("Found my UserData in pool."));
                                _userData = (UserDataV1)itemPool;
                                return UserData;
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
                                    var userData = (UserDataV1)itemBase;
                                    var itemBaseBytes = userData.ToByteArrayForSign();
                                    var itemPubKeys = UserPublicKey.Recover(itemBaseBytes, userData.Signature);

                                    foreach (var itemPubKey in itemPubKeys)
                                    {
                                        if (itemPubKey.SequenceEqual(userPubKey))
                                        {
                                            _logger.Information(string.Format("Found my UserData in pool."));
                                            _userData = userData;
                                            return UserData;
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
    }
}
