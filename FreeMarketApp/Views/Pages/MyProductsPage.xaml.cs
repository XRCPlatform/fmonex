using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FreeMarketApp.Views.Pages
{
    public class MyProductsPage : UserControl
    {
        private static MyProductsPage _instance;
        public static MyProductsPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyProductsPage();
                return _instance;
            }
        }

        public MyProductsPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
