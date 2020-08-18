using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.Views.Controls;

namespace FreeMarketApp.Views.Pages
{
    public class ChatPage : UserControl
    {
        private static ChatPage _instance;
        public static ChatPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ChatPage();
                return _instance;
            }
        }

        public ChatPage()
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

        public void ButtonRemove_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            MessageBox.Show(mainWindow, "Test", "Test title", MessageBox.MessageBoxButtons.YesNoCancel);
        }
    }
}
