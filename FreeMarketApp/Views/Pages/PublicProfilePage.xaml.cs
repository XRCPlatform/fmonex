using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketOne.ServerCore;
using Serilog;

namespace FreeMarketApp.Views.Pages
{
    public class PublicProfilePage : UserControl
    {
        private static PublicProfilePage _instance;
        private ILogger _logger;

        public static PublicProfilePage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PublicProfilePage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public PublicProfilePage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(PublicProfilePage).Namespace, typeof(PublicProfilePage).Name));

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
