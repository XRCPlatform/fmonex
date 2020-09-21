using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Chat;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.ServerCore;
using Libplanet.Crypto;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace FreeMarketOne.ServerCore
{
    public class ChatManager
    {
        public enum ChatItemTypeEnum
        {
            Buyer = 0,
            Seller = 1
        }

        private IBaseConfiguration _configuration;

        private ILogger _logger { get; set; }

        private readonly object _locked = new object();

        public ChatManager(IBaseConfiguration configuration)
        {
            _logger = Log.Logger.ForContext<ChatManager>();
            _logger.Information("Initializing Chat Manager");

            _configuration = configuration;
        }

        /// <summary>
        /// Generate a new chat data
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        public ChatDataV1 CreateNewChat(MarketItemV1 offer)
        {
            var result = new ChatDataV1();
            result.DateCreated = DateTime.UtcNow;
            result.MarketItem = offer;
            return result;
        }

        /// <summary>
        /// Saving a new chat data in data folder
        /// </summary>
        /// <param name="chatData"></param>
        public void SaveChat(ChatDataV1 chatData)
        {
            var fullPath = CheckExistenceOfFolder();

            _logger.Information(string.Format("Saving chat data."));

            var privateKey = FreeMarketOneServer.Current.UserManager.PrivateKey.ByteArray;
            byte[] keyBytes = new byte[32];
            Array.Copy(privateKey, keyBytes, keyBytes.Length);
            var aes = new SymmetricKey(keyBytes);

            var serializedChatData = JsonConvert.SerializeObject(chatData);
            var compressedChatData = ZipHelper.Compress(serializedChatData);

            var encryptedChatData = aes.Encrypt(compressedChatData);

            var pathKey = Path.Combine(fullPath, chatData.MarketItem.Signature);

            File.WriteAllBytes(pathKey, encryptedChatData);
        }

        /// <summary>
        /// Load chat by his signature from drive
        /// </summary>
        /// <param name="signature"></param>
        /// <returns></returns>
        public ChatDataV1 GetChat(string signature)
        {
            try
            {
                var fullPath = CheckExistenceOfFolder();
                var fileChatPath = Path.Combine(fullPath, signature);

                if (File.Exists(fileChatPath))
                {
                    var privateKey = FreeMarketOneServer.Current.UserManager.PrivateKey.ByteArray;
                    byte[] keyBytes = new byte[32];
                    Array.Copy(privateKey, keyBytes, keyBytes.Length);
                    var aes = new SymmetricKey(keyBytes);

                    var chatBytes = File.ReadAllBytes(fileChatPath);
                    var decryptedChatData = aes.Decrypt(chatBytes);
                    var serializedChatData = ZipHelper.Decompress(decryptedChatData);

                    return JsonConvert.DeserializeObject<ChatDataV1>(serializedChatData);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e.Message + " " + e.StackTrace);
            }

            return null;
        }

        /// <summary>
        /// Load all chats from drive
        /// </summary>
        /// <returns></returns>
        public List<ChatDataV1> GetAllChats()
        {
            var fullPath = CheckExistenceOfFolder();
            var result = new List<ChatDataV1>();
            DirectoryInfo d = new DirectoryInfo(fullPath);

            foreach (var file in d.GetFiles("*"))
            {
                var chat = GetChat(file.Name);
                if (chat != null) result.Add(chat);
            }

            return result;
        }

        /// <summary>
        /// Checking existence of our private folder
        /// </summary>
        /// <returns></returns>
        private string CheckExistenceOfFolder()
        {
            var folderPath = Path.Combine(_configuration.FullBaseDirectory, _configuration.ChatPath);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            return folderPath;
        }
    }
}
