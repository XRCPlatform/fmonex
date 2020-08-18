using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Views.Controls;
using System.Threading.Tasks;

namespace FreeMarketApp.Views.Pages
{
    public class MyProductsPage : UserControl
    {
        private static MyProductsPage _instance;
        public static MyProductsPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyProductsPage();
                return _instance;
            }
        }

        public MyProductsPage()
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

        public void ButtonRemove_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            MessageBox.Show(mainWindow, "Test", "Test title", MessageBox.MessageBoxButtons.YesNoCancel);
        }

        public void ButtonAdd_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, AddEditProductPage.Instance);
        }

        public void ButtonEdit_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, AddEditProductPage.Instance);
        }

        public void ButtonProduct_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyItemPage.Instance);
        }
    }
}
