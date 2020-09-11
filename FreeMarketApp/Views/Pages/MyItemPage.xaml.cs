using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.ServerCore;
using Serilog;

namespace FreeMarketApp.Views.Pages
{
    public class MyItemPage : UserControl
    {
        private static MyItemPage _instance;
        private ILogger _logger;

        public static MyItemPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyItemPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
        public static MyItemPage GetInstance()
        {
            return _instance;
        }

        public MyItemPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MyItemPage).Namespace, typeof(MyItemPage).Name));

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

        public void ButtonRemove_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            MessageBox.Show(mainWindow, "Test", "Test title", MessageBox.MessageBoxButtons.YesNoCancel);
        }
    }
}
