using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

        public void MyButton_Click(object sender, RoutedEventArgs args)
        {
            ((Button)sender).Content = "New text";
        }
    }
}
