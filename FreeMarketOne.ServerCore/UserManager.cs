using FreeMarketOne.DataStructure;
using Libplanet;
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
        private IBaseConfiguration _configuration;
        private bool _isValid;
        private ILogger _logger { get; set; }

        public bool IsValid => _isValid;

        public UserManager(IBaseConfiguration configuration)
        {
            _logger = Log.Logger.ForContext<ServiceManager>();
            _logger.Information("Initializing User Manager");

            _configuration = configuration;
            _isValid = false;
        }

        internal bool Initialize()
        {
            var filePath = Path.Combine(_configuration.FullBaseDirectory, _configuration.BlockChainSecretPath);

            if (File.Exists(filePath))
            {
                try
                {
                    var keyBytes = File.ReadAllBytes(filePath);
                    var key = new UserPrivateKey(keyBytes);
                    _logger.Information(string.Format("Node Public Key : {0}", key.PublicKey.KeyParam.Q.GetEncoded()));
                    _isValid = true;
                }
                catch (Exception)
                {
                    _isValid = false;
                }
            } 
            else
            {
                _isValid = false;
            }

            return _isValid;
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
    }
}
