using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketOne.ServerCore;
using Serilog;

namespace FreeMarketApp.Views.Pages
{
    public class ProductPage : UserControl
    {
        private static ProductPage _instance;
        private ILogger _logger;

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
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(ProductPage).Namespace, typeof(ProductPage).Name));

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
