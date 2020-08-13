using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
    }
}
