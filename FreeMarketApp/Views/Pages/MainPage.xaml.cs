using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketOne.ServerCore;
using Serilog;

namespace FreeMarketApp.Views.Pages
{
    public class MainPage : UserControl
    {
        private static MainPage _instance;
        private ILogger _logger;

        public static MainPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MainPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public MainPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MainPage).Namespace, typeof(MainPage).Name));

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
