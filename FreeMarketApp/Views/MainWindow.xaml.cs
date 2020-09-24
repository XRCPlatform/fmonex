using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views.Pages;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.Linq;

namespace FreeMarketApp.Views
{
    public class MainWindow : WindowBase
    {
        private ILogger _logger;

        public MainWindow()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MainWindow).Namespace, typeof(MainWindow).Name));

            Application.Current.Styles.Add(ThemeHelper.GetTheme());

            InitializeComponent();

            if (FreeMarketOneServer.Current.UserManager != null)
            {
                FreeMarketOneServer.Current.FreeMarketOneServerLoadedEvent += ServerLoadedEvent;

                var pcMainContent = this.FindControl<Panel>("PCMainContent");

                if (FreeMarketOneServer.Current.UserManager.PrivateKeyState == UserManager.PrivateKeyStates.Valid)
                {
                    PagesHelper.Log(_logger, "Private Key is valid adding MainPage instance.");

                    pcMainContent.Children.Add(MainPage.Instance);

                    PagesHelper.UnlockTools(this, true);
                    PagesHelper.SetUserData(_logger, this);
                    PagesHelper.SetServerData(_logger, this);
                }
                else
                {
                    PagesHelper.Log(_logger, "Private Key is not valid. Showing fist or login page.");

                    if ((FreeMarketOneServer.Current.UserManager.PrivateKeyState == UserManager.PrivateKeyStates.NoPassword)
                        || (FreeMarketOneServer.Current.UserManager.PrivateKeyState == UserManager.PrivateKeyStates.WrongPassword))
                    {
                        pcMainContent.Children.Add(LoginPage.Instance);
                    }
                    else
                    {
                        pcMainContent.Children.Add(FirstRunPage.Instance);
                    }

                    PagesHelper.UnlockTools(this, false);
                }
            }

            this.FixWindowCenterPosition();
            DataContextChanged += (object sender, EventArgs wat) =>
            {
                //reaction on data context change
            };
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonPrivateChat_Click(object sender, RoutedEventArgs args)
        {
            var chatPage = ChatPage.Instance;
            var chatItems = ((ChatPageViewModel)chatPage.DataContext).Items;

            if (chatItems.Any())
            {
                chatPage.LoadChatByProduct(chatItems.First().MarketItem.Signature);
            }

            PagesHelper.Switch(this, chatPage);
        }

        public void ButtonMyProducts_Click(object sender, RoutedEventArgs args)
        {
            PagesHelper.Switch(this, MyProductsPage.Instance);
            ChatPage.Instance = null;
        }

        public void ButtonSearch_Click(object sender, RoutedEventArgs args)
        {
            PagesHelper.Switch(this, SearchResultsPage.Instance);
            ChatPage.Instance = null;
        }

        public void ButtonMyProfile_Click(object sender, RoutedEventArgs args)
        {
            PagesHelper.Switch(this, MyProfilePage.Instance);
            ChatPage.Instance = null;
        }

        private void ServerLoadedEvent(object sender, EventArgs e)
        {
            PagesHelper.Log(_logger, "ServerLoadedEvent on MainWindow was raised.");

            Dispatcher.UIThread.InvokeAsync(() => { 
                PagesHelper.SetUserData(_logger, this);
                PagesHelper.SetServerData(_logger, this);
            });
        }

        public void ButtonSettings_Click(object sender, RoutedEventArgs args)
        {
            var settingsPage = SettingsPage.Instance;
            
            Panel panel = this.FindControl<Panel>("PCMainContent");
            if (panel.Children.Any()) {
                settingsPage.SetReturnTo((UserControl)panel.Children.First());
            }

            PagesHelper.Switch(this, settingsPage);
            ChatPage.Instance = null;
        }
    }
}
