using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;

namespace FreeMarketApp.Views.Pages
{
    public class PublicProfilePage : UserControl
    {
        private static PublicProfilePage _instance;
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
