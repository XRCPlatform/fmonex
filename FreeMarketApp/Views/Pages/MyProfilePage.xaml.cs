﻿using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;

namespace FreeMarketApp.Views.Pages
{
    public class MyProfilePage : UserControl
    {
        private static MyProfilePage _instance;
        public static MyProfilePage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyProfilePage();
                return _instance;
            }
        }

        public MyProfilePage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            Panel panel = mainWindow.FindControl<Panel>("PanelContent");

            if (!panel.Children.Contains(MainPage.Instance)) panel.Children.Add(MainPage.Instance);
            if (panel.Children.Contains(MyProfilePage.Instance)) panel.Children.Remove(MyProfilePage.Instance);
            if (panel.Children.Contains(MyProductsPage.Instance)) panel.Children.Remove(MyProductsPage.Instance);
            if (panel.Children.Contains(ChatPage.Instance)) panel.Children.Remove(ChatPage.Instance);
        }
    }
}
