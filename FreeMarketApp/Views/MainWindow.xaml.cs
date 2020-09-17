﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Threading;
using FreeMarketApp.Helpers;
using FreeMarketApp.Views.Pages;
using FreeMarketOne.Search;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace FreeMarketApp.Views
{
    public class MainWindow : WindowBase
    {
        private SearchEngine searchEngine;
        public MainWindow()
        {
            Application.Current.Styles.Add(ThemeHelper.GetTheme());

            InitializeComponent();

            if (FreeMarketOneServer.Current.UserManager != null)
            {
                FreeMarketOneServer.Current.FreeMarketOneServerLoadedEvent += ServerLoadedEvent;
                var pcMainContent = this.FindControl<Panel>("PCMainContent");

                if (FreeMarketOneServer.Current.UserManager.PrivateKeyState == UserManager.PrivateKeyStates.Valid)
                {
                    pcMainContent.Children.Add(MainPage.Instance);

                    PagesHelper.UnlockTools(this, true);
                    PagesHelper.SetUserData(this);
                }
                else
                {
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

        public void ButtonSearch_Click(object sender, RoutedEventArgs args)
        {
            var searchField = this.FindControl<TextBox>("SearchField");
            string searchText = searchField.Text;
            var engine = FreeMarketOneServer.Current.SearchEngine;
            var result = engine.Search(engine.ParseQuery(searchText));
            PagesHelper.Switch(this, SearchResultsPage.Instance);
        }

        public void ButtonMyProfile_Click(object sender, RoutedEventArgs args)
        {
            PagesHelper.Switch(this, MyProfilePage.Instance);
        }

        private void ServerLoadedEvent(object sender, EventArgs e)
        {
            Dispatcher.UIThread.InvokeAsync(() => { 
                PagesHelper.SetUserData(this);
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
