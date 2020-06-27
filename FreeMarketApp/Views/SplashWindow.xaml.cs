using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FreeMarketApp.Views
{
    public class SplashWindow : WindowBase
    {
        public SplashWindow()
        {
          
            InitializeComponent();
            this.FixWindowCenterPosition();
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
