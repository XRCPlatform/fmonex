using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FreeMarketApp.Helpers;
using FreeMarketApp.Resources;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views.Pages;
using FreeMarketOne.Users;
using Serilog;
using System;
using System.Linq;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views
{
    public class MainWindow : WindowBase
    {
        private ILogger _logger;

        public MainWindow()
        {
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MainWindow).Namespace, typeof(MainWindow).Name));

            Application.Current.Styles.Add(ThemeHelper.GetTheme());

            InitializeComponent();

            if (FMONE.Current.Users != null)
            {
                FMONE.Current.FreeMarketOneServerLoadedEvent += ServerLoadedEvent;
                var pcMainContent = this.FindControl<Panel>("PCMainContent");

                if (FMONE.Current.Users.PrivateKeyState == UserManager.PrivateKeyStates.Valid)
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

                    if ((FMONE.Current.Users.PrivateKeyState == UserManager.PrivateKeyStates.NoPassword)
                        || (FMONE.Current.Users.PrivateKeyState == UserManager.PrivateKeyStates.WrongPassword))
                    {
                        pcMainContent.Children.Add(LoginPage.Instance);

                        var tbUserName = this.FindControl<TextBlock>("TBUserName");
                        tbUserName.Text = SharedResources.ResourceManager.GetString("LoginPage_UserName_LoginName");
                    }
                    else
                    {
                        pcMainContent.Children.Add(FirstRunPage.Instance);
                    }

                    PagesHelper.UnlockTools(this, false);
                }

                var tbVersion = this.FindControl<TextBlock>("TBVersion");
                tbVersion.Text = FMONE.Current.Configuration.Version;
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
                chatPage.LoadChatByProduct(chatItems.First().MarketItem.Hash);
            }

            PagesHelper.Switch(this, chatPage);
        }

        public void ButtonMyProducts_Click(object sender, RoutedEventArgs args)
        {
            PagesHelper.Switch(this, MyProductsPage.Instance);
        }
        public void SearchTextbox_keyDownEvent(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                var searchField = this.FindControl<TextBox>("TBSearchField");

                string searchText = searchField.Text;
                if (searchText != null)
                {
                    searchText = searchText.Replace("*", "").Replace("?", "");
                }
                if (SearchResultsPage.ValidateQuery(searchText))
                {
                    SearchResultsPage.ResetInstance();
                    SearchResultsPage.SetSearchPhrase(searchText);

                    PagesHelper.Switch(this, SearchResultsPage.Instance);
                }

            }
        }

        public void ButtonSearch_Click(object sender, RoutedEventArgs args)
        {
            var searchField = this.FindControl<TextBox>("TBSearchField");

            string searchText = searchField.Text;
            if (searchText != null)
            {
                searchText = searchText.Replace("*", "").Replace("?", "");
            }
            if (SearchResultsPage.ValidateQuery(searchText))
            {
                SearchResultsPage.ResetInstance();
                SearchResultsPage.SetSearchPhrase(searchText);

                PagesHelper.Switch(this, SearchResultsPage.Instance);
            }            
        }

        public void ButtonMyProfile_Click(object sender, RoutedEventArgs args)
        {
            PagesHelper.Switch(this, MyProfilePage.Instance);
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

                var pageInstance = panel.Children.Last();

                if (pageInstance.GetType() == typeof(AddEditProductPage)) {
                    settingsPage.SetReturnTo(MyProductsPage.GetInstance());
                } 
                else if (pageInstance.GetType() == typeof(EditProfilePage)) {
                    settingsPage.SetReturnTo(MyProfilePage.GetInstance());
                }
                else if (pageInstance.GetType() == typeof(SearchResultsPage)) {
                    settingsPage.SetReturnTo(MainPage.GetInstance());
                } 
                else
                {
                    settingsPage.SetReturnTo((UserControl)panel.Children.Last());
                }
            }

            PagesHelper.Switch(this, settingsPage);
        }
    }
}
