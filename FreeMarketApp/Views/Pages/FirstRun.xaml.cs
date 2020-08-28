using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketOne.ServerCore;

namespace FreeMarketApp.Views.Pages
{
    public class FirstRunPage : UserControl
    {
        private static FirstRunPage _instance;
        public static FirstRunPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FirstRunPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public FirstRunPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void ButtonSave_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProfilePage.Instance);
        }

        public void ButtonRandomSeed_Click(object sender, RoutedEventArgs args)
        {
            var tbSeed = this.FindControl<TextBox>("TBSeed");
            tbSeed.Text = FreeMarketOneServer.Current.UserManager.CreateRandomSeed();
        }
    }
}
