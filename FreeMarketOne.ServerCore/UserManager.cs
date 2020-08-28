using FreeMarketOne.DataStructure;
using Libplanet;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
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

        private ILogger _logger { get; set; }

        public PrivateKeyStates PrivateKeyState => _privateKeyState;

        public UserPrivateKey PrivateKey => _privateKey;

        public UserManager(IBaseConfiguration configuration)
        {
            _logger = Log.Logger.ForContext<ServiceManager>();
            _logger.Information("Initializing User Manager");

            _configuration = configuration;
            _privateKeyState = PrivateKeyStates.NoKey;
        }

        internal PrivateKeyStates Initialize(string password = null)
        {
            var filePath = Path.Combine(_configuration.FullBaseDirectory, _configuration.BlockChainSecretPath);

            if (File.Exists(filePath))
            {
                try
                {
                    var keyBytes = File.ReadAllBytes(filePath);

                    if (password != null)
                    {
                        password = password.Substring(0, 16);
                        var key = password + password; //expecting 32 chars
                        var aes = new SymmetricKey(Encoding.ASCII.GetBytes(key));
                        var decryptedPrivKey = aes.Decrypt(keyBytes);

                        _privateKey = new UserPrivateKey(decryptedPrivKey);
                        _privateKeyState = PrivateKeyStates.Valid;

                        _logger.Information(string.Format("Private Key Decrypted"));
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
            string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
            Random random = new Random();

            char[] chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = validChars[random.Next(0, validChars.Length)];
            }

            return new string(chars);
        }

        public bool IsTextValid(string text)
        {
            string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
         
            if (string.IsNullOrEmpty(text))
            {
                return false;
            } 
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    var charTest = text.Substring(i, 1);
                    if (!validChars.Contains(charTest)) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Storing private key dat of user account
        /// </summary>
        /// <param name="seed">Expecting at least 200 chars.</param>
        /// <param name="password">Expecting 16 chars.</param>
        public void SaveNewPrivKey(string seed, string password, string path)
        {
            _logger.Information(string.Format("Saving new private key"));

            password = password.Substring(0, 16);
            var newUserPrivKey = new UserPrivateKey(seed);
            var key = password + password; //expecting 32 chars

            var aes = new SymmetricKey(Encoding.ASCII.GetBytes(key));
            var encryptedPrivKey = aes.Encrypt(newUserPrivKey.ByteArray);

            var directoryPath = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directoryPath);
            File.WriteAllBytes(path, encryptedPrivKey);
        }
    }
}
