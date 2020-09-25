using FreeMarketApp.Helpers;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Chat;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.ServerCore;
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
        private CancellationTokenSource _cancellationToken { get; set; }
        private IAsyncLoopFactory _asyncLoopFactory { get; set; }
        /// <summary>
        /// 0: Not started, 1: Running, 2: Stopping, 3: Stopped, 4: Mining
        /// </summary>
        private long _running;

        private ILogger _logger { get; set; }

        private readonly object _locked = new object();

        private const int RequestTimeout = 5000; // ms

        public bool IsRunning => Interlocked.Read(ref _running) == 1;

        public ChatManager(IBaseConfiguration configuration)
        {
            _logger = Log.Logger.ForContext<ChatManager>();
            _logger.Information("Initializing Chat Manager");
            
            _asyncLoopFactory = new AsyncLoopFactory(_logger);
            _configuration = configuration;
            _cancellationToken = new CancellationTokenSource();
        }

        public bool IsChatManagerRunning()
        {
            if (Interlocked.Read(ref _running) == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Start()
        {
            Interlocked.Exchange(ref _running, 1);

            StartMQListener();

            IAsyncLoop periodicLogLoop = this._asyncLoopFactory.Run("ChatManagerChecker", (cancellation) =>
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
                                    var chatPeerIp = chat.ChatItems[i].Type == (int)ChatItemTypeEnum.Seller ?
                                                    chat.MarketItem.BuyerOnionEndpoint : chat.SellerEndPoint;
                                    var endPoint = GetChatPeerEndpoint(chatPeerIp);

                                    //DELETE: endPoint = EndPointHelper.ParseIPEndPoint("127.0.0.1:9115");
                         
                                    periodicCheckLog.AppendLine(string.Format("Trying to send chat message to {0}.", endPoint.ToString()));
                                    _logger.Information(string.Format("Trying to send chat message to {0}.", endPoint.ToString()));

                                    if (SendNQMessage(chat.ChatItems[i], chat.MarketItem.Signature, endPoint))
                                    {
                                        periodicCheckLog.AppendLine(string.Format("Sending chat message done."));
                                        _logger.Information(string.Format("Sending chat message done."));

                                        chat.ChatItems[i].Propagated = true;
                                        anyChange = true;
                                    } 
                                    else
                                    {
                                        periodicCheckLog.AppendLine(string.Format("Chat peer is offline."));
                                        _logger.Information(string.Format("Chat peer is offline."));
                                    }
                                }
                            }
                        }

                        if (anyChange) SaveChat(chat);
                    }
                }

                if (periodicCheckLog.Length > 0)
                {
                    var periodicCheckLogResult = new StringBuilder();

                    periodicCheckLogResult.AppendLine("======Chat Manager Check====== " + dateTimeUtc.ToString(CultureInfo.InvariantCulture));
                    periodicCheckLogResult.Append(periodicCheckLog.ToString());
                    Console.WriteLine(periodicCheckLogResult.ToString());
                }

                return Task.CompletedTask;
            },
            _cancellationToken.Token,
            repeatEvery: TimeSpans.HalfMinute,
            startAfter: TimeSpans.HalfMinute);
        }

        /// <summary>
        /// Add port to chat peer endpoint
        /// </summary>
        /// <param name="peerIp"></param>
        /// <returns></returns>
        private IPEndPoint GetChatPeerEndpoint(string peerIp)
        {
            var endPoint = EndPointHelper.ParseIPEndPoint(peerIp);
            endPoint.Port = _configuration.ListenerChatEndPoint.Port;

            return endPoint;
        }

        private static void ClientOnReceiveReady(object sender, NetMQSocketEventArgs args)
        {
            //Console.WriteLine("Server replied ({0})", args.Socket.ReceiveFrameString());
        }

        /// <summary>
        /// Send message by NetMQ
        /// </summary>
        /// <param name="chatItem"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        private bool SendNQMessage(ChatItem chatItem, string signature, IPEndPoint endPoint)
        {
            var connectionString = string.Format("tcp://{0}", endPoint.ToString());

            using (var client = new RequestSocket())
            {
                try
                {
                    var chatMessage = new ChatMessage();
                    chatMessage.Message = chatItem.Message;
                    chatMessage.ExtraMessage = chatItem.ExtraMessage;
                    chatMessage.DateCreated = chatItem.DateCreated;
                    chatMessage.Signature = signature;

                    client.Options.Linger = TimeSpan.Zero;

                    client.Connect(connectionString);

                    client.SendMultipartMessage(chatMessage.ToNetMQMessage());
                    client.ReceiveReady += ClientOnReceiveReady;
                    bool pollResult = client.Poll(TimeSpan.FromMilliseconds(RequestTimeout));
                    client.ReceiveReady -= ClientOnReceiveReady;
                    client.Disconnect(connectionString);
                    
                    return pollResult;
                }
                catch (Exception ex)
                {
                    _logger.Error(
                        ex,
                        $"An unexpected exception occurred during SendNQMessage(): {ex}"
                    );
                }
            }

            return false;
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
            var aes = new SymmetricKey(Encoding.ASCII.GetBytes(password));
            var first = true;

            foreach (var item in chatItems)
            {
                if (first)
                {
                    first = false;
                    continue;
                }

                item.Message = Encoding.UTF8.GetString(aes.Decrypt(Convert.FromBase64String(item.Message)));
                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// TODO DEBUG TOOL DELETE TOO
        /// </summary>
        /// <param name="operationType"></param>
        /// <param name="message"></param>
        private void PrintFrames(string operationType, NetMQMessage message)
        {
            for (int i = 0; i < message.FrameCount; i++)
            {
                Console.WriteLine("{0} Socket : Frame[{1}] = {2}", operationType, i,
                    message[i].ConvertToString());
            }
        }

        /// <summary>
        /// Load separate chat listener ovwe NetMQ
        /// </summary>
        private void StartMQListener()
        {
            Task.Run(() =>
            {
                var endPoint = _configuration.ListenerChatEndPoint.ToString();

                using (var response = new ResponseSocket())
                {
                    response.Options.Linger = TimeSpan.Zero;
                    Console.WriteLine("Binding {0}", endPoint);
                    response.Bind(endPoint);

                    while (true)
                    {
                        var clientMessage = response.ReceiveMultipartMessage(4);

                        Console.WriteLine("Msg received!");
                        _logger.Information("Receiving chat message from peer.");

                        var receivedChatItem = new ChatMessage(clientMessage);

                        var allLocalChats = GetAllChats();
                        var localChat = allLocalChats.FirstOrDefault(a => a.MarketItem.Signature == receivedChatItem.Signature);
                        if (localChat != null)
                        {
                            var newChatItem = new ChatItem();
                            newChatItem.DateCreated = receivedChatItem.DateCreated;
                            newChatItem.Message = receivedChatItem.Message;
                            newChatItem.ExtraMessage = receivedChatItem.ExtraMessage;
                            newChatItem.Type = (int)DetectWhoIm(localChat);
                            newChatItem.Propagated = true;

                            localChat.ChatItems.Add(newChatItem);
                            localChat.SellerEndPoint = receivedChatItem.ExtraMessage;

                            _logger.Information("Saving local chat with new data.");

                            SaveChat(localChat);
                        }

                        response.SendMultipartMessage(clientMessage);

                        Thread.Sleep(1000);
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
            result.SellerEndPoint = _configuration.ListenerChatEndPoint.Address.MapToIPv4().ToString();
            result.ChatItems = new List<ChatItem>();

            var newInitialMessage = new ChatItem();
            var textHelper = new TextHelper();
            newInitialMessage.Message = textHelper.GetRandomText(32);
            newInitialMessage.ExtraMessage = result.SellerEndPoint;
            newInitialMessage.DateCreated = DateTime.UtcNow;
            newInitialMessage.Type = (int)ChatItemTypeEnum.Seller;

            result.ChatItems.Add(newInitialMessage);
            return result;
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

                var privateKey = FreeMarketOneServer.Current.UserManager.PrivateKey.ByteArray;
                byte[] keyBytes = new byte[32];
                Array.Copy(privateKey, keyBytes, keyBytes.Length);
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
            try
            {
                var fullPath = CheckExistenceOfFolder();
                var fileChatPath = Path.Combine(fullPath, hash);

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
        public void PrepaireMessageToWorker(ChatDataV1 chatData, string message)
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
                    newChatItem.Type = (int)DetectWhoIm(chatData);

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
        private ChatItemTypeEnum DetectWhoIm(ChatDataV1 chatData)
        {
            var marketItem = chatData.MarketItem;

            var userManager = FreeMarketOneServer.Current.UserManager;
            var userPubKey = userManager.GetCurrentUserPublicKey();
            var marketItemBytes = marketItem.ToByteArrayForSign();

            var signaturePubKeys = UserPublicKey.Recover(marketItemBytes, marketItem.Signature);

            foreach (var itemPubKey in signaturePubKeys)
            {
                if (itemPubKey.SequenceEqual(userPubKey))
                {
                    return ChatItemTypeEnum.Seller;
                }
            }

            return ChatItemTypeEnum.Buyer;
        }

        /// <summary>
        /// Process new accepted block... if it is market item and it is mine then create a new chat
        /// </summary>
        /// <param name="block"></param>
        public void ProcessNewBlock(Block<MarketAction> block)
        {
            var types = new Type[] { typeof(MarketItemV1) };

            _logger.Information(string.Format("Processing a new block {0}.", block.Hash));

            var userPubKey = FreeMarketOneServer.Current.UserManager.GetCurrentUserPublicKey();

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
                                        var newChat = FreeMarketOneServer.Current.ChatManager.CreateNewSellerChat((MarketItemV1)itemMarket);
                                        FreeMarketOneServer.Current.ChatManager.SaveChat(newChat);
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
            Interlocked.Exchange(ref _running, 2);

            _cancellationToken.Cancel();

            Interlocked.Exchange(ref _running, 3);
        }
    }
}
