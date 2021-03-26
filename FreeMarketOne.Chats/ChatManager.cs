using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Chat;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Markets;
using FreeMarketOne.Search;
using FreeMarketOne.Tor;
using FreeMarketOne.Users;
using Libplanet;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Extensions;
using Libplanet.Net;
using Libplanet.Net.Messages;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FreeMarketOne.Extensions.Common.ServiceHelper;

namespace FreeMarketOne.Chats
{
    public class ChatManager : IChatManager
    {
        public enum ChatItemTypeEnum
        {
            Buyer = 0,
            Seller = 1
        }

        private IBaseConfiguration _configuration;
        private CancellationTokenSource _cancellationToken { get; set; }
        private IAsyncLoopFactory _asyncLoopFactory { get; set; }
        private UserPrivateKey _userPrivateKey { get; set; }
        private IUserManager _userManager { get; set; }
        private TimeSpan _repeatEvery { get; set; }
        private TimeSpan _startAfter { get; set; }
        private string _serverOnionAddress { get; set; }

        private readonly TorSocks5Transport transport;

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private CommonStates _running;
        
        private static readonly object _fileLock = new object();

        private ILogger _logger { get; set; }

        public bool IsRunning => _running == CommonStates.Running;

        private readonly SearchEngine _searchEngine;

        public ChatManager(IBaseConfiguration configuration,
            AppProtocolVersion protocolVersion,
            UserPrivateKey privateKey,
            IUserManager userManager,
            TorSocks5Manager torSocksManager,
            TorProcessManager torProcessManager,
            SearchEngine searchEngine,
            TimeSpan? repeatEvery = null,
            TimeSpan? startAfter = null)
        {
            _logger = Log.Logger.ForContext<ChatManager>();
            _logger.Information("Initializing Chat Manager");

            _asyncLoopFactory = new AsyncLoopFactory(_logger);
            _configuration = configuration;
            _cancellationToken = new CancellationTokenSource();
            _userPrivateKey = privateKey;
            _userManager = userManager;
            _serverOnionAddress = torProcessManager.TorOnionEndPoint;
            _searchEngine = searchEngine;

            transport = new TorSocks5Transport(_userPrivateKey, protocolVersion, null, null, null, torProcessManager.TorOnionEndPoint, 9115, null, ReceivedMessageHandler, _logger, torSocksManager, torProcessManager, null);

            if (!repeatEvery.HasValue)
            {
                _repeatEvery = TimeSpans.HalfMinute;
            }
            else
            {
                _repeatEvery = repeatEvery.Value;
            }

            if (!startAfter.HasValue)
            {
                _startAfter = TimeSpans.HalfMinute;
            }
            else
            {
                _startAfter = startAfter.Value;
            }
        }

        private static string Hash(string rawData)
        {
            return Hash(Encoding.UTF8.GetBytes(rawData));
        }

        private static string Hash(byte[] rawData)
        {
            using (SHA256 shaHash = SHA256.Create())
            {
                byte[] bytes = shaHash.ComputeHash(rawData);
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        /// <summary>
        /// Sender's identity, private key possession for peers pubkey is validated in transport layer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void ReceivedMessageHandler(object sender, ReceivedRequestEventArgs message)
        {
            TotClient client = message.Client;
            ChatDataV1 localChat = null;
            string pubKeyHash = Hash(message.Peer.PublicKey.Format(false));
            Console.WriteLine($"Receiving chat message from peer: {message.Peer}.");
            _logger.Information($"Receiving chat message from peer: {message.Peer}.");

            var receivedChatItem = message.Envelope.GetBody<ChatItem>();

            _logger.Information("Loading new chat message.");
            if (receivedChatItem.MarketItemHash == null)
            {
                localChat = GetChat(pubKeyHash);
            }

            //although chats are re-created on both sides, there could be race conditions were IBD is ongoing but chat message has been received
            //the chat could have been deleted if someone reset the whole folder
            //as chat was created on a buyer side during checkout process
            //allowing resuming conversation by creating chat if it does not exist
            if (localChat == null)
            {
                _logger.Information("Did  not find historic chat, ceating new on recieved message");
                //there may be a case for offers that are past market chain longevity
                //var result  = _searchEngine.GetMyCompletedOffers(OfferDirection.Bought, 1000, 0);
                var result = _searchEngine.Search(_searchEngine.BuildQueryByItemHash(receivedChatItem.MarketItemHash), false, 1);
                if (result != null)
                {
                    MarketItem marketItem = result.Results.FirstOrDefault();
                    //MarketItem marketItem = result.Results.Find(m => m.Hash == receivedChatItem.MarketItemHash);
                    if (marketItem != null)
                    {
                        localChat = CreateNewChat((MarketItemV1)marketItem);
                        AppendToChat(receivedChatItem, localChat);
                    }
                    else
                    {
                        _logger.Information($"Could not find Market item hash {receivedChatItem.MarketItemHash} in search index.");
                    }
                }
            }
            else
            {
                AppendToChat(receivedChatItem, localChat);
            }
            try
            {
                var validated = DecryptAndVerifyChatItem(localChat.ChatItems, receivedChatItem);
                //store digest for retransmition
                receivedChatItem.Digest = validated.Digest;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failure decrypting and validating message digest.");
            }

            _logger.Information("Sending ACK response.");
            transport.ReplyMessage<ChatItem>(message.Request, client, receivedChatItem);
        }

        private void AppendToChat(ChatItem receivedChatItem, ChatDataV1 localChat)
        {
            if (String.IsNullOrEmpty(receivedChatItem.Digest))
            {
                throw new ArgumentException("Recieved item is misding digest.");
            }
            var newChatItem = new ChatItem();
            newChatItem.DateCreated = receivedChatItem.DateCreated;
            newChatItem.Message = receivedChatItem.Message;
            newChatItem.ExtraMessage = receivedChatItem.ExtraMessage;
            newChatItem.Type = (int)DetectWhoIm(localChat, false);
            newChatItem.Propagated = true;
            newChatItem.Digest = receivedChatItem.Digest;

            _logger.Information("Add a new item to chat item.");
            if (localChat.ChatItems == null) localChat.ChatItems = new List<ChatItem>();

            //avoid duplicate system retry[s]  
            var found = localChat.ChatItems.Find(c => c.Digest.Equals(receivedChatItem.Digest)
                                                   && c.DateCreated.Equals(receivedChatItem.DateCreated));
            if (found == null)
            {
                localChat.ChatItems.Add(newChatItem);
            }

            if (!string.IsNullOrEmpty(receivedChatItem.ExtraMessage))
            {
                localChat.SellerEndPoint = receivedChatItem.ExtraMessage;
            }

            _logger.Information("Saving local chat with new data.");

            SaveChat(localChat);
        }


        /// <summary>
        /// Is change manager running
        /// </summary>
        /// <returns></returns>
        public bool IsChatManagerRunning()
        {
            if (_running == CommonStates.Running)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Start loop task to listen chat messages
        /// </summary>
        public async Task Start()
        {
            _running = CommonStates.Running;

            await transport.StartAsync();

            IAsyncLoop periodicLogLoop = this._asyncLoopFactory.Run("ChatManagerChecker", async (cancellation) =>
            {
                var dateTimeUtc = DateTime.UtcNow;

                StringBuilder periodicCheckLog = new StringBuilder();
                var chats = GetAllChats();
                if (chats.Any())
                {
                    foreach (var chat in chats)
                    {
                        var anyChange = false;

                        if ((chat.ChatItems != null) && (chat.ChatItems.Any()))
                        {
                            for (int i = 0; i < chat.ChatItems.Count; i++)
                            {
                                if (!chat.ChatItems[i].Propagated)
                                {
                                    string chatPeerIp = null;
                                    List<byte[]> pubKeys;
                                    if (chat.ChatItems[i].Type == (int)ChatItemTypeEnum.Seller)
                                    {
                                        chatPeerIp = chat.MarketItem.BuyerOnionEndpoint;
                                        pubKeys = UserPublicKey.Recover(chat.MarketItem.ToByteArrayForSign(), chat.MarketItem.BuyerSignature);
                                    }
                                    else
                                    {
                                        chatPeerIp = chat.SellerEndPoint;
                                        pubKeys = UserPublicKey.Recover(chat.MarketItem.ToByteArrayForSign(), chat.MarketItem.Signature);
                                    }
                                    PublicKey publicKey = new PublicKey(pubKeys.FirstOrDefault());

                                    var endPoint = GetChatPeerEndpoint(chatPeerIp);
                                    var peer = new BoundPeer(publicKey, endPoint);

                                    chat.ChatItems[i].MarketItemHash = chat.MarketItem.Hash;

                                    periodicCheckLog.AppendLine(string.Format("Trying to send chat message to {0}.", endPoint.ToString()));
                                    _logger.Information(string.Format("Trying to send chat message to {0}.", endPoint.ToString()));
                                    try
                                    {
                                        ///there seem to be a problem here that mesages go to the drain
                                        var response = await transport.SendMessageWithReplyAsync<ChatItem, ChatItem>(peer, chat.ChatItems[i], TimeSpan.FromSeconds(5));
                                        periodicCheckLog.AppendLine(string.Format("Sending chat message done."));
                                        _logger.Information(string.Format("Sending chat message done."));
                                        if (!response.Digest.Equals(chat.ChatItems[i].Digest))
                                        {
                                            throw new FailedMessageDigestValidation("Message digest after transmition to remote is not the same. Crypto error.");
                                        }
                                        chat.ChatItems[i].Propagated = true;
                                        anyChange = true;
                                    }
                                    catch (Exception e)
                                    {
                                        periodicCheckLog.AppendLine("Chat peer is offline.");
                                        _logger.Information($"Chat peer is offline. {e}");
                                    }
                                }
                            }

                            if (anyChange)
                            {
                                //is this not reseting? where did it save
                                var savedChat = GetChat(chat.MarketItem.Hash);
                                for (int i = 0; i < chat.ChatItems.Count; i++)
                                {
                                    savedChat.ChatItems[i] = chat.ChatItems[i];
                                }
                                SaveChat(savedChat);
                            }
                        }
                    }
                }

                if (periodicCheckLog.Length > 0)
                {
                    var periodicCheckLogResult = new StringBuilder();

                    periodicCheckLogResult.AppendLine("======Chat Manager Check====== " + dateTimeUtc.ToString(CultureInfo.InvariantCulture));
                    periodicCheckLogResult.Append(periodicCheckLog.ToString());
                    Console.WriteLine(periodicCheckLogResult.ToString());
                }

                return;
            },
            _cancellationToken.Token,
            repeatEvery: _repeatEvery,
            startAfter: _startAfter);
        }

        /// <summary>
        /// Add port to chat peer endpoint
        /// </summary>
        /// <param name="peerIp"></param>
        /// <returns></returns>
        private DnsEndPoint GetChatPeerEndpoint(string onionAddress)
        {
            DnsEndPoint endPoint = new DnsEndPoint(onionAddress, _configuration.ListenerChatEndPoint.Port);
            return endPoint;
        }


        public ChatItem DecryptAndVerifyChatItem(List<ChatItem> chatItems, ChatItem lastItem)
        {
            var password = chatItems.First().Message;
            var aes = new SymmetricKey(Encoding.UTF8.GetBytes(password));
            ChatItem processedItem = DecryptChatItem(aes, lastItem);
            return processedItem;
        }

        /// <summary>
        /// Descrypt chat items in chat
        /// </summary>
        /// <param name="chatItems"></param>
        /// <returns></returns>
        public List<ChatItem> DecryptChatItems(List<ChatItem> chatItems)
        {
            var result = new List<ChatItem>();

            var password = chatItems.First().Message;
            var aes = new SymmetricKey(Encoding.UTF8.GetBytes(password));
            var first = true;

            foreach (var item in chatItems)
            {
                if (first)
                {
                    first = false;
                    continue;
                }

                try
                {
                    ChatItem processedItem = DecryptChatItem(aes, item);
                    result.Add(processedItem);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"An unexpected exception occurred during DecryptChatItems(): {ex}");
                }
            }

            return result;
        }

        private ChatItem DecryptChatItem(SymmetricKey aes, ChatItem item)
        {
            var processedItem = new ChatItem();
            processedItem.DateCreated = item.DateCreated;
            processedItem.ExtraMessage = item.ExtraMessage;
            processedItem.Propagated = item.Propagated;
            processedItem.Type = item.Type;
            processedItem.Message = Encoding.UTF8.GetString(aes.Decrypt(Convert.FromBase64String(item.Message)));
            processedItem.Digest = Hash(processedItem.Message);
            if (!processedItem.Digest.Equals(item.Digest))
            {
                _logger.Warning($"Failed message digest validation. Expected:[{item.Digest}] actual:{processedItem.Digest}, most likely due to cryptographic failures.");
                throw new FailedMessageDigestValidation("Failed to decrypt messages.");
            }

            return processedItem;
        }

        /// <summary>
        /// Checking if we can start with chat
        /// </summary>
        /// <param name="chatItems"></param>
        /// <returns></returns>
        public bool IsChatValid(List<ChatItem> chatItems)
        {
            if (chatItems.First().Propagated)
            {
                return true;
            }
            else
            {
                return false;
            }
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
        /// Generate a new chat data at seller side
        /// </summary>
        /// <param name="offer"></param>
        /// <returns></returns>
        public ChatDataV1 CreateNewSellerChat(MarketItemV1 offer)
        {
            var result = CreateNewChat(offer);
            result.SellerEndPoint = _serverOnionAddress;
            result.ChatItems = new List<ChatItem>();

            var newInitialMessage = new ChatItem();
            newInitialMessage.Message = GetRandomText(32);
            newInitialMessage.ExtraMessage = result.SellerEndPoint;
            newInitialMessage.DateCreated = DateTime.UtcNow;
            newInitialMessage.Type = (int)ChatItemTypeEnum.Seller;
            newInitialMessage.MarketItemHash = offer.Hash;
            newInitialMessage.Digest = Hash(newInitialMessage.Message);
            result.ChatItems.Add(newInitialMessage);
            return result;
        }

        public string GetRandomText(int length)
        {
            var stringChars = new char[length];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = TextHelper.CHARS[random.Next(TextHelper.CHARS.Length)];
            }

            return new string(stringChars);
        }

        /// <summary>
        /// Saving a new chat data in data folder
        /// </summary>
        /// <param name="chatData"></param>
        public void SaveChat(ChatDataV1 chatData)
        {
            lock (_fileLock)
            {
                var fullPath = CheckExistenceOfFolder();

                _logger.Information(string.Format("Saving chat data."));

                byte[] keyBytes = new byte[32];
                Array.Copy(_userPrivateKey.ByteArray, keyBytes, keyBytes.Length);
                var aes = new SymmetricKey(keyBytes);

                var serializedChatData = JsonConvert.SerializeObject(chatData);
                var compressedChatData = ZipHelper.Compress(serializedChatData);

                var encryptedChatData = aes.Encrypt(compressedChatData);

                var pathKey = Path.Combine(fullPath, chatData.MarketItem.Hash);

                try
                {
                    File.WriteAllBytes(pathKey, encryptedChatData);
                }
                catch (Exception e)
                {
                    _logger.Error(e,$" Failed saving file {pathKey}. Will retry in in 1s.");
                    //give a chance for file to be released
                    Thread.Sleep(1000);
                    //retry as this could happen in race condition when chat is read and written at same time
                    File.WriteAllBytes(pathKey, encryptedChatData);
                }
                
            }
        }

        /// <summary>
        /// Load chat by his hash from drive
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public ChatDataV1 GetChat(string hash)
        {
            lock (_fileLock)
            {
                try
                {
                    var fullPath = CheckExistenceOfFolder();
                    var fileChatPath = Path.Combine(fullPath, hash);

                    if (File.Exists(fileChatPath))
                    {
                        var privateKey = _userPrivateKey.ByteArray;
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

        /// <summary>
        /// Verification agains spamming
        /// </summary>
        /// <param name="chatData"></param>
        /// <returns></returns>
        public bool CanSendNextMessage(ChatDataV1 chatData)
        {
            if (chatData != null)
            {
                if (chatData.ChatItems.Any())
                {
                    var lastChatItem = chatData.ChatItems.Last();
                    if (lastChatItem.DateCreated.AddSeconds(10) > DateTime.UtcNow)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Add message to our worker
        /// </summary>
        /// <param name="chatData"></param>
        /// <param name="message"></param>
        public void PrepareMessage(ChatDataV1 chatData, string message)
        {
            if (chatData != null)
            {
                if (chatData.ChatItems.Any())
                {
                    var password = chatData.ChatItems.First().Message;
                    var aes = new SymmetricKey(Encoding.UTF8.GetBytes(password));
                    byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                    var newChatItem = new ChatItem();
                    newChatItem.Message = Convert.ToBase64String(aes.Encrypt(messageBytes));
                    newChatItem.DateCreated = DateTime.UtcNow;
                    newChatItem.Type = (int)DetectWhoIm(chatData, true);
                    newChatItem.ExtraMessage = string.Empty; //not used - maybe for future
                    newChatItem.MarketItemHash = chatData.MarketItem.Hash;
                    newChatItem.Digest = Hash(messageBytes);
                    chatData.ChatItems.Add(newChatItem);

                    SaveChat(chatData);
                }
            }
        }

        /// <summary>
        /// Detect ownership of market item from Chat Data
        /// </summary>
        /// <param name="chatData"></param>
        /// <returns></returns>
        private ChatItemTypeEnum DetectWhoIm(ChatDataV1 chatData, bool isMyMessage)
        {
            _logger.Information(string.Format("Detecting whoIm."));

            if ((chatData.ChatItems != null) && (chatData.ChatItems.Any()))
            {
                var firstItem = chatData.ChatItems.First();

                if (isMyMessage)
                {
                    if (firstItem.Type == (int)ChatItemTypeEnum.Seller)
                    {
                        _logger.Information(string.Format("Im Seller {0}", isMyMessage));
                        return ChatItemTypeEnum.Seller;
                    }
                    else
                    {
                        _logger.Information(string.Format("Im Buyer. {0}", isMyMessage));
                        return ChatItemTypeEnum.Buyer;
                    }
                }
                else
                {
                    if (firstItem.Type == (int)ChatItemTypeEnum.Seller)
                    {
                        _logger.Information(string.Format("Im Buyer. {0}", isMyMessage));
                        return ChatItemTypeEnum.Buyer;
                    }
                    else
                    {
                        _logger.Information(string.Format("Im Seller {0}", isMyMessage));
                        return ChatItemTypeEnum.Seller;
                    }
                }
            }
            else
            {
                //case of first message (first is message from seller because of it we are buyer)
                _logger.Information(string.Format("Im Buyer. {0}", isMyMessage));
                return ChatItemTypeEnum.Buyer;
            }
        }

        /// <summary>
        /// Process new accepted block... if it is market item and it is mine then create a new chat
        /// </summary>
        /// <param name="block"></param>
        public void ProcessNewBlock(Block<MarketAction> block)
        {
            var types = new Type[] { typeof(MarketItemV1) };

            _logger.Information(string.Format("Processing a new block {0}.", block.Hash));

            var userPubKey = _userPrivateKey.PublicKey.KeyParam.Q.GetEncoded();

            foreach (var itemTx in block.Transactions)
            {
                foreach (var itemAction in itemTx.Actions)
                {
                    foreach (var itemMarket in itemAction.BaseItems)
                    {
                        if (types.Contains(itemMarket.GetType()))
                        {
                            var marketData = (MarketItemV1)itemMarket;
                            if (marketData.BuyerSignature != null && marketData.State == (int)MarketManager.ProductStateEnum.Sold)
                            {
                                var itemMarketBytes = itemMarket.ToByteArrayForSign();
                                var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, marketData.Signature);

                                foreach (var itemPubKey in itemPubKeys)
                                {
                                    if (itemPubKey.SequenceEqual(userPubKey))
                                    {
                                        //id chats were preserved by leaving chat folder don't create new chat password and etc
                                        var found = GetChat(itemMarket.Hash);
                                        if (found == null)
                                        {
                                            _logger.Information(string.Format("Creating a new chat for item {0}.", itemMarket.Hash));
                                            var newChat = CreateNewSellerChat((MarketItemV1)itemMarket);
                                            SaveChat(newChat);
                                        }
                                    }
                                }

                                var buyerPubKeys = UserPublicKey.Recover(itemMarketBytes, marketData.BuyerSignature);
                                foreach (var itemPubKey in buyerPubKeys)
                                {
                                    if (itemPubKey.SequenceEqual(userPubKey))
                                    {
                                        //id chats were preserved by leaving chat folder don't create new chat password and etc
                                        var found = GetChat(itemMarket.Hash);
                                        if (found == null)
                                        {
                                            _logger.Information(string.Format("Creating a new chat for item {0}.", itemMarket.Hash));
                                            var newChat = CreateNewChat((MarketItemV1)itemMarket);
                                            SaveChat(newChat);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            transport.StopAsync(TimeSpan.FromSeconds(1), _cancellationToken.Token).ConfigureAwait(false).GetAwaiter().GetResult();
            _running = CommonStates.Stopping;

            _cancellationToken.Cancel();

            _running = CommonStates.Stopped;
        }
    }
}
