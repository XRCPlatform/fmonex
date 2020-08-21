using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;

namespace FreeMarketApp.Views.Pages
{
    public class AddEditProfilePage : UserControl
    {
        private static AddEditProfilePage _instance;
        public static AddEditProfilePage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AddEditProfilePage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public AddEditProfilePage()
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

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);
        }

        public void ButtonMyReviews_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyReviewsPage.Instance);
        }

        public void ButtonMyProfile_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);
        }

        public void ButtonCancel_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);
        }
    }
}
