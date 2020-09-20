using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketOne.ServerCore;
using Serilog;
using System.Linq;
using System.Threading;

namespace FreeMarketApp.Views.Pages
{
    public class MyBoughtProductsPage : UserControl
    {
        private static MyBoughtProductsPage _instance;
        private ILogger _logger;

        public static MyBoughtProductsPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyBoughtProductsPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static MyBoughtProductsPage GetInstance()
        {
            return _instance;
        }

        public MyBoughtProductsPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MyBoughtProductsPage).Namespace, typeof(MyBoughtProductsPage).Name));

            if ((FreeMarketOneServer.Current.MarketManager != null) && (FreeMarketOneServer.Current.UserManager != null))
            {
                SpinWait.SpinUntil(() => FreeMarketOneServer.Current.GetServerState() == FreeMarketOneServer.FreeMarketOneServerStates.Online);

                PagesHelper.Log(_logger, string.Format("Loading my bought market offers from chain."));

                var userPubKey = FreeMarketOneServer.Current.UserManager.GetCurrentUserPublicKey();

                //my own offers or sells
                var myBoughtOffers = FreeMarketOneServer.Current.MarketManager.GetAllBuyerMarketItemsByPubKeys(userPubKey);
                SkynetHelper.PreloadTitlePhotos(myBoughtOffers, _logger);

                DataContext = new MyBoughtProductsPageViewModel(myBoughtOffers);
            }

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

            ClearForm();
        }
        
        public void ButtonMyProducts_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProductsPage.Instance);

            ClearForm();
        }

        public void ButtonBoughtProduct_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            var signature = ((Button)sender).Tag.ToString();

            var marketItem = ((MyBoughtProductsPageViewModel)this.DataContext).Items.FirstOrDefault(a => a.Signature == signature);
            if ((marketItem != null) && (!marketItem.IsInPool))
            {
                var chatPage = ChatPage.Instance;
                chatPage.LoadChatByProduct(signature);

                PagesHelper.Switch(mainWindow, chatPage);
            }
        }

        private void ClearForm()
        {
            _instance = null;
        }
    }
}
