using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using FreeMarketApp.Helpers;
using FreeMarketApp.Views.Pages;
using FreeMarketOne.Search;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.Linq;

namespace FreeMarketApp.Views
{
    public class MainWindow : WindowBase
    {
        private ILogger _logger;
        private SearchEngine searchEngine;

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
            PagesHelper.Switch(this, ChatPage.Instance);
        }

        public void ButtonMyProducts_Click(object sender, RoutedEventArgs args)
        {
            PagesHelper.Switch(this, MyProductsPage.Instance);
        }
        public void SearchTextbox_keyDownEvent(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                var searchField = this.FindControl<TextBox>("SearchField");

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
            var searchField = this.FindControl<TextBox>("SearchField");

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
        }
    }
}
