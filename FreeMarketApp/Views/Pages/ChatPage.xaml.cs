using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
    }
}
