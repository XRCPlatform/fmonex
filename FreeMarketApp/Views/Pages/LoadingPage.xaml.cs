using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FreeMarketApp.Views.Pages
{
    public class LoadingPage : UserControl
    {
        private static LoadingPage _instance;

        public static LoadingPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new LoadingPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        public static LoadingPage GetInstance()
        {
            return _instance;
        }


        public LoadingPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
