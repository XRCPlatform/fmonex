using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FreeMarketApp.Views.Pages
{
    public class MainPage : UserControl
    {
        private static MainPage _instance;
        public static MainPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MainPage();
                return _instance;
            }
        }

        public MainPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
