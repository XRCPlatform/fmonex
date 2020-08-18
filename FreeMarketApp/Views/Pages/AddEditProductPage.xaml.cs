using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;

namespace FreeMarketApp.Views.Pages
{
    public class AddEditProductPage : UserControl
    {
        private static AddEditProductPage _instance;
        public static AddEditProductPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AddEditProductPage();
                return _instance;
            }
        }

        public AddEditProductPage()
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

            PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
        }

        public void ButtonCancel_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProductsPage.Instance);
        }
    }
}
