using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
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
            panel.Children.Add(MainPage.Instance);

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
            Panel panel = this.FindControl<Panel>("PanelContent");
            if (!panel.Children.Contains(ChatPage.Instance)) panel.Children.Add(ChatPage.Instance);
            if (panel.Children.Contains(MainPage.Instance)) panel.Children.Remove(MainPage.Instance);
            if (panel.Children.Contains(MyProductsPage.Instance)) panel.Children.Remove(MyProductsPage.Instance);
            if (panel.Children.Contains(MyProfilePage.Instance)) panel.Children.Remove(MyProfilePage.Instance);
        }

        public void ButtonMyProducts_Click(object sender, RoutedEventArgs args)
        {
            Panel panel = this.FindControl<Panel>("PanelContent");
            if (!panel.Children.Contains(MyProductsPage.Instance)) panel.Children.Add(MyProductsPage.Instance);
            if (panel.Children.Contains(MainPage.Instance)) panel.Children.Remove(MainPage.Instance);
            if (panel.Children.Contains(ChatPage.Instance)) panel.Children.Remove(ChatPage.Instance);
            if (panel.Children.Contains(MyProfilePage.Instance)) panel.Children.Remove(MyProfilePage.Instance);
        }

        public void ButtonSearch_Click(object sender, RoutedEventArgs args)
        {

        }

        public void ButtonMyProfile_Click(object sender, RoutedEventArgs args)
        {
            Panel panel = this.FindControl<Panel>("PanelContent");
            if (!panel.Children.Contains(MyProfilePage.Instance)) panel.Children.Add(MyProfilePage.Instance);
            if (panel.Children.Contains(MainPage.Instance)) panel.Children.Remove(MainPage.Instance);
            if (panel.Children.Contains(MyProductsPage.Instance)) panel.Children.Remove(MyProductsPage.Instance);
            if (panel.Children.Contains(ChatPage.Instance)) panel.Children.Remove(ChatPage.Instance);
        }
    }
}
