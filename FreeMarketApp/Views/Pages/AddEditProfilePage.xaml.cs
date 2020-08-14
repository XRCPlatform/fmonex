﻿using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;

namespace FreeMarketApp.Views.Pages
{
    public class AddEditProfilePage : UserControl
    {
        private static AddEditProfilePage _instance;
        public static AddEditProfilePage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AddEditProfilePage();
                return _instance;
            }
        }

        public AddEditProfilePage()
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

            PagesHelper.Switch(mainWindow, MainPage.Instance);
        }
    }
}