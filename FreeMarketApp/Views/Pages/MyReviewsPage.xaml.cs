using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;

namespace FreeMarketApp.Views.Pages
{
    public class MyReviewsPage : UserControl
    {
        private static MyReviewsPage _instance;
        public static MyReviewsPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyReviewsPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public MyReviewsPage()
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

        public void ButtonMyProfile_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);
        }
    }
}
