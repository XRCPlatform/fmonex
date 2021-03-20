using Avalonia.Remote.Protocol;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Chat;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.Markets;
using FreeMarketOne.P2P;
using FreeMarketOne.Users;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Extensions;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FreeMarketOne.Extensions.Common.ServiceHelper;
using Libplanet.Net;
using FreeMarketOne.DataStructure.ProtocolVersions;
using FreeMarketOne.Tor;
using Libplanet.Net.Messages;

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

        private string _socks5Proxy;
        private readonly TorSocks5Transport transport;

        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped
        /// </summary>
        private CommonStates _running;

        private ILogger _logger { get; set; }

        private readonly object _locked = new object();

        private const int RequestTimeout = 5000; // ms

        public bool IsRunning => _running == CommonStates.Running;

        public ChatManager(IBaseConfiguration configuration,
            AppProtocolVersion protocolVersion,
            UserPrivateKey privateKey,
            IUserManager userManager,
            TorSocks5Manager torSocksManager,
            TorProcessManager torProcessManager,
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

        private void ReceivedMessageHandler(object sender, ReceivedRequestEventArgs message)
        {
            TotClient client = message.Client;

            Console.WriteLine($"Receiving chat message from peer: {message.Peer}.");
            _logger.Information($"Receiving chat message from peer: {message.Peer}.");

            var receivedChatItem = message.Envelope.GetBody<ChatItem>();

            _logger.Information("Loading new chat message.");
            var localChat = GetChat(receivedChatItem.MarketItemHash);
            if (localChat != null)
            {
                var newChatItem = new ChatItem();
                newChatItem.DateCreated = receivedChatItem.DateCreated;
                newChatItem.Message = receivedChatItem.Message;
                newChatItem.ExtraMessage = receivedChatItem.ExtraMessage;
                newChatItem.Type = (int)DetectWhoIm(localChat, false);
                newChatItem.Propagated = true;

                _logger.Information("Add a new item to chat item.");
                if (localChat.ChatItems == null) localChat.ChatItems = new List<ChatItem>();
                localChat.ChatItems.Add(newChatItem);

                if (!string.IsNullOrEmpty(receivedChatItem.ExtraMessage))
                {
                    localChat.SellerEndPoint = receivedChatItem.ExtraMessage;
                }

                _logger.Information("Saving local chat with new data.");

                SaveChat(localChat);
            }

            _logger.Information("Returning data.");
            //original round tripped the message figure out why?
            transport.ReplyMessage<ChatItem>(message.Request, client, receivedChatItem);
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
                                        chatPeerIp =  chat.MarketItem.BuyerOnionEndpoint;
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
                                        await transport.SendMessageWithReplyAsync<ChatItem, ChatItem>(peer, chat.ChatItems[i], TimeSpan.FromMinutes(1));
                                        periodicCheckLog.AppendLine(string.Format("Sending chat message done."));
                                        _logger.Information(string.Format("Sending chat message done."));

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

        /// <summary>
        /// This can be deleted is it for development
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        //private static void ClientOnReceiveReady(object sender, NetMQSocketEventArgs args)
        //{
        //    Console.WriteLine("Server replied ({0})", args.Socket.ReceiveFrameString());
        //}

        /// <summary>
        /// Send message by NetMQ
        /// </summary>
        /// <param name="chatItem"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        //private bool SendNQMessage(ChatItem chatItem, string hash, DnsEndPoint endPoint)
        //{
        //    var connectionString = ToNetMQAddress(endPoint.Host);
        //    using (var client = new RequestSocket(connectionString))//should connection be passed here?
        //    {
        //        try
        //        {
        //            var chatMessage = new ChatMessage();
        //            chatMessage.Message = chatItem.Message;
        //            chatMessage.ExtraMessage = chatItem.ExtraMessage;
        //            chatMessage.DateCreated = chatItem.DateCreated;
        //            chatMessage.Hash = hash;

        //            client.Connect(connectionString);

        //            client.SendMultipartMessage(chatMessage.ToNetMQMessage());
        //            client.ReceiveReady += ClientOnReceiveReady;
        //            bool pollResult = client.Poll(TimeSpan.FromMilliseconds(RequestTimeout));
        //            client.ReceiveReady -= ClientOnReceiveReady;
        //            client.Disconnect(connectionString);

        //            return pollResult;
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.Error(
        //                ex,
        //                $"An unexpected exception occurred during SendNQMessage(): {ex}"
        //            );
        //        }
        //    }

        //    return false;
        //}

        /// <summary>
        /// Descrypt chat items in chat
        /// </summary>
        /// <param name="chatItems"></param>
        /// <returns></returns>
        public List<ChatItem> DecryptChatItems(List<ChatItem> chatItems)
        {
            var result = new List<ChatItem>();

            var password = chatItems.First().Message;
            var aes = new SymmetricKey(Encoding.ASCII.GetBytes(password));
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
                    var processedItem = new ChatItem();
                    processedItem.DateCreated = item.DateCreated;
                    processedItem.ExtraMessage = item.ExtraMessage;
                    processedItem.Propagated = item.Propagated;
                    processedItem.Type = item.Type;
                    processedItem.Message = Encoding.UTF8.GetString(aes.Decrypt(Convert.FromBase64String(item.Message)));
                    result.Add(processedItem);
                }
                catch (Exception )
                {

                }
            }

            return result;
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

        //private string ToNetMQAddress(string onionAddress)
        //{
        //    return string.IsNullOrEmpty(_socks5Proxy) ?
        //        $"tcp://{onionAddress}:{_configuration.ListenerChatEndPoint.Port}" :
        //        $"socks5://{_socks5Proxy};{onionAddress}:{_configuration.ListenerChatEndPoint.Port}";
        //}

        /// <summary>
        /// Load separate chat listener over NetMQ
        /// </summary>
        private void StartMQListener()
        {
            Task.Run(() =>
            {
                var endPoint = _configuration.ListenerChatEndPoint.ToString();
                var connectionString = string.Format("tcp://{0}", endPoint.ToString());

                using (var response = new ResponseSocket())
                {
                    response.Options.Linger = TimeSpan.Zero;
                    Console.WriteLine("Chat listener binding {0}", endPoint);
                    response.Bind(connectionString);

                    while (true)
                    {
                        var clientMessage = response.ReceiveMultipartMessage(4);//why 4 frames?

                        Console.WriteLine("Receiving chat message from peer.");
                        _logger.Information("Receiving chat message from peer.");

                        var receivedChatItem = new ChatMessage(clientMessage);

                        _logger.Information("Loading new chat message.");
                        var localChat = GetChat(receivedChatItem.Hash);
                        if (localChat != null)
                        {
                            var newChatItem = new ChatItem();
                            newChatItem.DateCreated = receivedChatItem.DateCreated;
                            newChatItem.Message = receivedChatItem.Message;
                            newChatItem.ExtraMessage = receivedChatItem.ExtraMessage;
                            newChatItem.Type = (int)DetectWhoIm(localChat, false);
                            newChatItem.Propagated = true;

                            _logger.Information("Add a new item to chat item.");
                            if (localChat.ChatItems == null) localChat.ChatItems = new List<ChatItem>();
                            localChat.ChatItems.Add(newChatItem);

                            if (!string.IsNullOrEmpty(receivedChatItem.ExtraMessage))
                            {
                                localChat.SellerEndPoint = receivedChatItem.ExtraMessage;
                            }

                            _logger.Information("Saving local chat with new data.");

                            SaveChat(localChat);
                        } 

                        _logger.Information("Returning data.");
                        response.SendMultipartMessage(clientMessage);
                    }
                }
            });
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
            var _object = new Object();

            lock (_object)
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

                File.WriteAllBytes(pathKey, encryptedChatData);
            }
        }

        /// <summary>
        /// Load chat by his hash from drive
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public ChatDataV1 GetChat(string hash)
        {
            var _object = new Object();

            lock (_object)
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
                    var aes = new SymmetricKey(Encoding.ASCII.GetBytes(password));

                    var newChatItem = new ChatItem();
                    newChatItem.Message = Convert.ToBase64String(aes.Encrypt(Encoding.UTF8.GetBytes(message)));
                    newChatItem.DateCreated = DateTime.UtcNow;
                    newChatItem.Type = (int)DetectWhoIm(chatData, true);
                    newChatItem.ExtraMessage = string.Empty; //not used - maybe for future
                    newChatItem.MarketItemHash = chatData.MarketItem.Hash;
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
                            var itemMarketBytes = itemMarket.ToByteArrayForSign();
                            var itemPubKeys = UserPublicKey.Recover(itemMarketBytes, marketData.Signature);

                            foreach (var itemPubKey in itemPubKeys)
                            {
                                if (itemPubKey.SequenceEqual(userPubKey))
                                {
                                    if (marketData.State == (int)MarketManager.ProductStateEnum.Sold)
                                    {
                                        _logger.Information(string.Format("Creating a new chat for item {0}.", itemMarket.Hash));
                                        var newChat = CreateNewSellerChat((MarketItemV1)itemMarket);
                                        SaveChat(newChat);
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
            _running = CommonStates.Stopping;

            _cancellationToken.Cancel();

            _running = CommonStates.Stopped;
        }
    }
}
