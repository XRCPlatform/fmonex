using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Views.Pages;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;

namespace FreeMarketApp.Views
{
    public class MainWindow : WindowBase
    {

        public MainWindow()
        {
            InitializeComponent();

            var panel = this.FindControl<Panel>("PanelContent");

            if (FreeMarketOneServer.Current.UserManager.PrivateKeyState == UserManager.PrivateKeyStates.Valid)
            {
                panel.Children.Add(MainPage.Instance);

                PagesHelper.UnlockTools(this, true);
            }
            else
            {
                if ((FreeMarketOneServer.Current.UserManager.PrivateKeyState == UserManager.PrivateKeyStates.NoPassword)
                    || (FreeMarketOneServer.Current.UserManager.PrivateKeyState == UserManager.PrivateKeyStates.WrongPassword))
                {
                    panel.Children.Add(LoginPage.Instance);
                }
                else
                {
                    panel.Children.Add(FirstRunPage.Instance);
                }

                PagesHelper.UnlockTools(this, false);
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
            PagesHelper.Switch(this, SearchResultsPage.Instance);
        }

        public void ButtonMyProfile_Click(object sender, RoutedEventArgs args)
        {
            PagesHelper.Switch(this, MyProfilePage.Instance);
        }
    }
}
