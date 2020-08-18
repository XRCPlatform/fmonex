using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;

namespace FreeMarketApp.Views.Pages
{
    public class MyProfilePage : UserControl
    {
        private static MyProfilePage _instance;
        public static MyProfilePage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyProfilePage();
                return _instance;
            }
        }

        public MyProfilePage()
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

        public void ButtonMyReviews_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyReviewsPage.Instance);
        }
        public void ButtonEdit_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, AddEditProfilePage.Instance);
        }
    }
}
