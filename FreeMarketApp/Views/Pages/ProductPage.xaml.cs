using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;

namespace FreeMarketApp.Views.Pages
{
    public class ProductPage : UserControl
    {
        private static ProductPage _instance;
        public static ProductPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ProductPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public ProductPage()
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
