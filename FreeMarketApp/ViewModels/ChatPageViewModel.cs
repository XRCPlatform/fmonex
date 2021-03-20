using FreeMarketOne.DataStructure.Chat;
using Libplanet.Net.Messages;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FreeMarketApp.ViewModels
{
    public class ChatPageViewModel : ViewModelBase
    {
        public ChatPageViewModel(IEnumerable<ChatDataV1> items)
        {
            Items = new ObservableCollection<ChatDataV1>(items);
            ChatItems = new ObservableCollection<ChatItem>();
        }

        public ObservableCollection<ChatDataV1> Items { get; set; }
        public ObservableCollection<ChatItem> ChatItems { get; set; }
    }
}
