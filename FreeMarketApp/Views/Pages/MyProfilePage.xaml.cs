using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketOne.ServerCore;
using Serilog;

namespace FreeMarketApp.Views.Pages
{
    public class MyProfilePage : UserControl
    {
        private static MyProfilePage _instance;
        private ILogger _logger;

        public static MyProfilePage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyProfilePage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public MyProfilePage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MyProfilePage).Namespace, typeof(MyProfilePage).Name));

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

        public void ButtonMyReviews_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyReviewsPage.Instance);
        }
        public void ButtonEdit_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, EditProfilePage.Instance);
        }
    }
}
