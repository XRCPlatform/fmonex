using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
