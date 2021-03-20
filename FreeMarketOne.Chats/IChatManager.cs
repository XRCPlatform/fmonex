using FreeMarketOne.DataStructure;
using FreeMarketOne.DataStructure.Chat;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using Libplanet.Blocks;
using Libplanet.Net.Messages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreeMarketOne.Chats
{
    public interface IChatManager
    {
        void Dispose();
        void SaveChat(ChatDataV1 chatData);
        ChatDataV1 CreateNewSellerChat(MarketItemV1 offer);
        ChatDataV1 CreateNewChat(MarketItemV1 offer);
        Task Start();
        void ProcessNewBlock(Block<MarketAction> block);
        bool IsChatManagerRunning();
        bool CanSendNextMessage(ChatDataV1 chatData);
        List<ChatDataV1> GetAllChats();
        List<ChatItem> DecryptChatItems(List<ChatItem> chatItems);
        void PrepareMessage(ChatDataV1 chatData, string message);
        bool IsChatValid(List<ChatItem> chatItems);
        ChatDataV1 GetChat(string hash);
    }
}
