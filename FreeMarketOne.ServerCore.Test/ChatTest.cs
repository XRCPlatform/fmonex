using FreeMarketOne.BlockChain;
using FreeMarketOne.BlockChain.Test.Helpers;
using FreeMarketOne.Chats;
using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Chat;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using FreeMarketOne.P2P;
using FreeMarketOne.Pools;
using FreeMarketOne.Users;
using Libplanet.Blockchain;
using Libplanet.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using static FreeMarketOne.Chats.ChatManager;

namespace FreeMarketOne.ServerCore.Test
{
    [TestClass]
    public class ChatTest
    {
        private IBaseConfiguration _configuration;
        private ILogger _logger;
        private IOnionSeedsManager _onionSeedsManager;
        private BasePoolManager _basePoolManager;
        private event EventHandler _baseBlockChainLoadedEvent;
        private event EventHandler<BlockChain<BaseAction>.TipChangedEventArgs> _baseBlockChainChangedEvent;
        private IBlockChainManager<BaseAction> _baseBlockChainManager;
        private ChatManager ChatManager;
        private UserPrivateKey _userPrivateKey;
        private UserManager _userManager;

        [TestMethod]
        public void TestChatConnection()
        {
            var debugEnvironment = new DebugEnvironmentHelper();
            _baseBlockChainLoadedEvent = null;
            _baseBlockChainChangedEvent = null;

            debugEnvironment.Initialize<ChatTest>(
                ref _configuration,
                ref _logger,
                ref _onionSeedsManager,
                ref _basePoolManager,
                ref _baseBlockChainManager,
                ref _userPrivateKey,
                ref _baseBlockChainLoadedEvent,
                ref _baseBlockChainChangedEvent);

            SpinWait.SpinUntil(() => _baseBlockChainManager.IsBlockChainManagerRunning());

            //User Manager
            _userManager = new UserManager(_configuration);
            _userManager.PrivateKey = _userPrivateKey;

            //Chat Manager
            ChatManager = new ChatManager(_configuration, _userPrivateKey, _userManager, _configuration.ListenerChatEndPoint.Address,
                TimeSpans.FiveSeconds, TimeSpans.TenSeconds);
            ChatManager.Start();

            SpinWait.SpinUntil(() => ChatManager.IsChatManagerRunning());

            var marketDemoData = new MarketItemV1();
            marketDemoData.BaseSignature = "DEMOBUYERSIGNATURE";
            marketDemoData.BuyerOnionEndpoint = _configuration.ListenerChatEndPoint.Address.MapToIPv4().ToString();
            marketDemoData.Title = "Test ITEM";
            marketDemoData.Hash = marketDemoData.GenerateHash();

            var bytesToSign = marketDemoData.ToByteArrayForSign();
            marketDemoData.Signature = Convert.ToBase64String(_userManager.PrivateKey.Sign(bytesToSign));

            var newChat = ChatManager.CreateNewSellerChat(marketDemoData);
            ChatManager.SaveChat(newChat);

            Thread.Sleep((int)TimeSpans.TenSeconds.TotalMilliseconds);
            SpinWait.SpinUntil(() => ChatManager.GetChat(newChat.MarketItem.Hash).ChatItems.Count > 1);
            SpinWait.SpinUntil(() => ChatManager.GetChat(newChat.MarketItem.Hash).ChatItems.First().Propagated == true);

            //item vas propagated to peer
            var syncedChat = ChatManager.GetChat(newChat.MarketItem.Hash);
            Assert.IsTrue(syncedChat.ChatItems.Count == 2);

            //propagate new chat message
            syncedChat = ChatManager.GetChat(newChat.MarketItem.Hash);
            ChatManager.PrepaireMessageToWorker(syncedChat, "this is a test");

            Thread.Sleep((int)TimeSpans.TenSeconds.TotalMilliseconds);
            SpinWait.SpinUntil(() => ChatManager.GetChat(newChat.MarketItem.Hash).ChatItems.Count > 3);
            SpinWait.SpinUntil(() => ChatManager.GetChat(newChat.MarketItem.Hash).ChatItems[2].Propagated == true);

            //loading latest chat data
            syncedChat = ChatManager.GetChat(newChat.MarketItem.Hash);
            var decryptedChat = ChatManager.DecryptChatItems(syncedChat.ChatItems); 

            //only two of them are correct because of it two
            Assert.IsTrue(decryptedChat.Count == 2);
        }
    }
}
