﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DynamicData;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.Extensions.Helpers;
using Serilog;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class ChatPage : UserControl
    {
        private static ChatPage _instance;
        private ILogger _logger;
        private UserControl _backPage;
        private string _activeChatHash;
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

        public string GetActiveChatHash()
        {
            return _instance._activeChatHash;
        }

        public ChatPage()
        {
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(ChatPage).Namespace, typeof(ChatPage).Name));

            this.InitializeComponent();

            if (FMONE.Current.Chats != null)
            {
                var chatManager = FMONE.Current.Chats;
                PagesHelper.Log(_logger, string.Format("Loading chats from data folder."));

                var chats = chatManager.GetAllChats();
                DataContext = new ChatPageViewModel(chats);

                FMONE.Current.Chats.NewChatReceivedEvent += new EventHandler<string>(NewChatReceivedEvent);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            if (_backPage != null)
            {
                PagesHelper.Switch(mainWindow, _backPage);
            }
            else
            {
                PagesHelper.Switch(mainWindow, MainPage.Instance);
            }

            ClearForm();
        }

        public void ButtonChat_Click(object sender, RoutedEventArgs args)
        {
            var hash = ((Button)sender).Tag.ToString();

            LoadChatByProduct(hash);
        }

        public void SetBackPage(UserControl back)
        {
            _backPage = back;
        }

        public async void ButtonSendMessage_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var hash = ((Button)sender).Tag.ToString();

            var errorCount = 0;
            var errorMessages = new StringBuilder();
            var textHelper = new TextHelper();

            var tbMessage = Instance.FindControl<TextBox>("TBMessage");
            if (string.IsNullOrEmpty(tbMessage.Text) || (tbMessage.Text.Length < 1))
            {
                errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_ChatPage_ShortMessage"));
                errorCount++;
            }
            else
            {
                if (!textHelper.IsTextNotDangerous(tbMessage.Text))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_ChatPage_InvalidCharsMessage"));
                    errorCount++;
                }

                if (!textHelper.IsWithoutBannedWords(tbMessage.Text))
                {
                    errorMessages.AppendLine(SharedResources.ResourceManager.GetString("Dialog_ChatPage_BannedWordsDescription"));
                    errorCount++;
                }
            }

            if (errorCount == 0)
            {
                var chatManager = FMONE.Current.Chats;
                var chatData = chatManager.GetChat(hash);
                if (chatData != null)
                {
                    if (string.IsNullOrEmpty(chatData.SellerEndPoint))
                    {
                        await MessageBox.Show(mainWindow,
                            string.Format(SharedResources.ResourceManager.GetString("Dialog_Information_ChatWaitForAnswer")),
                            SharedResources.ResourceManager.GetString("Dialog_Information_Title"),
                            MessageBox.MessageBoxButtons.Ok);
                    }
                    else
                    {
                        if (!chatManager.CanSendNextMessage(chatData))
                        {
                            await MessageBox.Show(mainWindow,
                                string.Format(SharedResources.ResourceManager.GetString("Dialog_Information_CantSendNextMessage")),
                                SharedResources.ResourceManager.GetString("Dialog_Information_Title"),
                                MessageBox.MessageBoxButtons.Ok);
                        }
                        else
                        {
                            chatManager.PrepaireMessageToWorker(chatData, tbMessage.Text);

                            LoadChatByProduct(hash);
                            tbMessage.Text = string.Empty;
                        }
                    }
                }
            } 
            else
            {
                await MessageBox.Show(mainWindow,
                    errorMessages.ToString(),
                    SharedResources.ResourceManager.GetString("Dialog_Information_Title"),
                    MessageBox.MessageBoxButtons.Ok);
            }
        }

        private void NewChatReceivedEvent(object sender, string hash)
        {
            if (_instance._activeChatHash == hash)
            {
                LoadChatByProduct(hash);
            }
        }

        public async void LoadChatByProduct(string hash)
        {
            var chatManager = FMONE.Current.Chats;
            var chatData = chatManager.GetChat(hash);
            _instance._activeChatHash = hash;

            if (chatData != null)
            {
                var btSendMessage = Instance.FindControl<Button>("BTSendMessage");
                var srTitle = Instance.FindControl<Separator>("SRTitle");
                var tbTitle = Instance.FindControl<TextBlock>("TBTitle");
                var tbMessage = Instance.FindControl<TextBox>("TBMessage");
                var btWithoutMessage = Instance.FindControl<Border>("BIWithoutMessage");
                var svChat = Instance.FindControl<ScrollViewer>("SVChat");

                btSendMessage.Tag = chatData.MarketItem.Hash;
                tbTitle.Text = chatData.MarketItem.Title;
                srTitle.IsVisible = true;

                ((ChatPageViewModel)DataContext).ChatItems.Clear();
                if ((chatData.ChatItems != null) && chatData.ChatItems.Any() && chatManager.IsChatValid(chatData.ChatItems))
                {
                    var decryptedChat = chatManager.DecryptChatItems(chatData.ChatItems);
                    ((ChatPageViewModel)DataContext).ChatItems.AddRange(decryptedChat);
                    btWithoutMessage.IsVisible = false;
                    btSendMessage.IsEnabled = true;
                    tbMessage.IsEnabled = true;
                } 
                else
                {
                    btWithoutMessage.IsVisible = true;
                    btSendMessage.IsEnabled = false;
                    tbMessage.IsEnabled = false;
                }

                await Task.Delay(TimeSpans.Ms100);
                svChat.Offset = new Vector(svChat.Offset.X, svChat.Extent.Height - svChat.Viewport.Height);
            }
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}
