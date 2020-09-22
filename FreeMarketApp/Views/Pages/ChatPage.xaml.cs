using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DynamicData;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure.Chat;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FreeMarketApp.Views.Pages
{
    public class ChatPage : UserControl
    {
        private static ChatPage _instance;
        private ILogger _logger;

        public static ChatPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ChatPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        public static ChatPage GetInstance()
        {
            return _instance;
        }

        public ChatPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(ChatPage).Namespace, typeof(ChatPage).Name));

            if (FreeMarketOneServer.Current.ChatManager != null)
            {
                var chatManager = FreeMarketOneServer.Current.ChatManager;
                PagesHelper.Log(_logger, string.Format("Loading chats from data folder."));

                var chats = chatManager.GetAllChats();
                DataContext = new ChatPageViewModel(chats);

                if (chats.Any())
                {
                    LoadChatByProduct(chats.First().MarketItem.Signature);
                }
            }

            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MainPage.Instance);

            ClearForm();
        }

        public void ButtonChat_Click(object sender, RoutedEventArgs args)
        {
            var signature = ((Button)sender).Tag.ToString();

            LoadChatByProduct(signature);
        }

        public void ButtonSendMessage_Click(object sender, RoutedEventArgs args)
        {
            var signature = ((Button)sender).Tag.ToString();
        }

        public void LoadChatByProduct(string signature)
        {
            var chatData = ((ChatPageViewModel)DataContext).Items.FirstOrDefault(a => a.MarketItem.Signature == signature);

            if (chatData != null)
            {
                var btSendMessage = Instance.FindControl<Button>("BTSendMessage");
                var srTitle = Instance.FindControl<Separator>("SRTitle");
                var tbTitle = Instance.FindControl<TextBlock>("TBTitle");
                var tbMessage = Instance.FindControl<TextBox>("TBMessage");

                btSendMessage.Tag = chatData.MarketItem.Signature;
                tbTitle.Text = chatData.MarketItem.Title;
                srTitle.IsVisible = true;

                ((ChatPageViewModel)DataContext).ChatItems.Clear();
                if ((chatData.ChatItems != null) && (chatData.ChatItems.Any()))
                {
                    ((ChatPageViewModel)DataContext).ChatItems.AddRange(chatData.ChatItems);
                }
            }
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}
