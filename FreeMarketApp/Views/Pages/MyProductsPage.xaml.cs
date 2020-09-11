using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.Threading.Tasks;

namespace FreeMarketApp.Views.Pages
{
    public class MyProductsPage : UserControl
    {
        private static MyProductsPage _instance;
        private ILogger _logger;

        public static MyProductsPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyProductsPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static MyProductsPage GetInstance()
        {
            return _instance;
        }

        public MyProductsPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MyProductsPage).Namespace, typeof(MyProductsPage).Name));

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

        public void ButtonAdd_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, AddEditProductPage.Instance);
        }

        public void ButtonProduct_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            var signature = ((Button)sender).Tag.ToString();
            var myProductItemPage = MyProductItemPage.Instance;
            myProductItemPage.LoadProduct(signature);

            PagesHelper.Switch(mainWindow, myProductItemPage);
        }
    }
}
