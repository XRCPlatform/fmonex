﻿using Avalonia;
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

            Panel panel = this.FindControl<Panel>("PanelContent");
            if (FreeMarketOneServer.Current.UserManager.IsValid)
            {
                panel.Children.Add(MainPage.Instance);
            } 
            else
            {
                panel.Children.Add(FirstRunPage.Instance);

                var btMyProfile = this.FindControl<Button>("BTMyProfile");
                btMyProfile.IsEnabled = false;
                var btSearch = this.FindControl<Button>("BTSearch");
                btSearch.IsEnabled = false;
                var btMyProducts = this.FindControl<Button>("BTMyProducts");
                btMyProducts.IsEnabled = false;
                var btPrivateChat = this.FindControl<Button>("BTPrivateChat");
                btPrivateChat.IsEnabled = false;
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
