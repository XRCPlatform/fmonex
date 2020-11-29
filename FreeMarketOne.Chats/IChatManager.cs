using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Chat;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.Blocks;
using System;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.Chats
{
    public interface IChatManager
    {
        void Dispose();
        void SaveChat(ChatDataV1 chatData);
        ChatDataV1 CreateNewSellerChat(MarketItemV1 offer);
        ChatDataV1 CreateNewChat(MarketItemV1 offer);
        void Start();
        void ProcessNewBlock(Block<MarketAction> block);
        bool IsChatManagerRunning();
        bool CanSendNextMessage(ChatDataV1 chatData);
        List<ChatDataV1> GetAllChats();
        List<ChatItem> DecryptChatItems(List<ChatItem> chatItems);
        void PrepaireMessageToWorker(ChatDataV1 chatData, string message);
        bool IsChatValid(List<ChatItem> chatItems);
    }
}
