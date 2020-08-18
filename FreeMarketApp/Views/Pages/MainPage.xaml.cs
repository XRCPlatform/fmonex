using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;

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

        public void ButtonProduct_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, ProductPage.Instance);
        }
    }
}
