using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FreeMarketOne.ServerCore;
using FreeMarketOne.ServerCore.ViewModels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMarketApp.Views
{
    public class SplashWindow : Window
    {
        public SplashWindow()
        {
          
            InitializeComponent();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
