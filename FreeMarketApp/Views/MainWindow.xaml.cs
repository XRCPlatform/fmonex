using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace FreeMarketApp.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
          
            InitializeComponent();
            DataContextChanged += (object sender, EventArgs wat) =>
            {
                // here, this.DataContext will be your MainWindowViewModel
            };
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
