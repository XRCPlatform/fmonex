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
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class ChatPage : UserControl
    {
        private static ChatPage _instance;
        private ILogger _logger;
        private UserControl backPage;

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
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            if (backPage != null)
            {
                PagesHelper.Switch(mainWindow, backPage);
            }
            else
            {
                PagesHelper.Switch(mainWindow, MainPage.Instance);
            }

            ClearForm();
        }

        public void ButtonChat_Click(object sender, RoutedEventArgs args)
        {
            var signature = ((Button)sender).Tag.ToString();

            LoadChatByProduct(signature);
        }

        public void SetBackPage(UserControl back)
        {
            backPage = back;
        }

        public async void ButtonSendMessage_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var signature = ((Button)sender).Tag.ToString();

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
            }

            if (errorCount == 0)
            {
                var chatData = ((ChatPageViewModel)DataContext).Items.FirstOrDefault(a => a.MarketItem.Signature == signature);
                if (chatData != null)
                {
                    var chatManager = FMONE.Current.Chats;

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

  
        public void LoadChatByProduct(string signature)
        {
            var chatData = ((ChatPageViewModel)DataContext).Items.FirstOrDefault(a => a.MarketItem.Signature == signature);

            if (chatData != null)
            {
                var btSendMessage = Instance.FindControl<Button>("BTSendMessage");
                var srTitle = Instance.FindControl<Separator>("SRTitle");
                var tbTitle = Instance.FindControl<TextBlock>("TBTitle");
                var tbMessage = Instance.FindControl<TextBox>("TBMessage");
                var btWithoutMessage = Instance.FindControl<Border>("BIWithoutMessage");

                btSendMessage.Tag = chatData.MarketItem.Signature;
                tbTitle.Text = chatData.MarketItem.Title;
                srTitle.IsVisible = true;

                ((ChatPageViewModel)DataContext).ChatItems.Clear();
                if ((chatData.ChatItems != null) && chatData.ChatItems.Any() && (chatData.ChatItems.Count > 1))
                {
                    var chatManager = FMONE.Current.Chats;

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
            }
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}
