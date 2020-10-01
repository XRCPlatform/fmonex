using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketOne.Markets;
using Serilog;
using System.Linq;
using System.Threading;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class MyProductsPage : UserControl
    {
        private static MyProductsPage _instance;
        private ILogger _logger;

        public static MyProductsPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MyProductsPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static MyProductsPage GetInstance()
        {
            return _instance;
        }

        public MyProductsPage()
        {
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MyProductsPage).Namespace, typeof(MyProductsPage).Name));

            this.InitializeComponent();

            if ((FMONE.Current.Markets != null) && (FMONE.Current.Users != null))
            {
                SpinWait.SpinUntil(() => FMONE.Current.GetServerState() == FMONE.FreeMarketOneServerStates.Online);

                PagesHelper.Log(_logger, string.Format("Loading my market offers from chain."));

                var userPubKey = FMONE.Current.Users.GetCurrentUserPublicKey();

                //my own offers or sells
                var myOffers = FMONE.Current.Markets.GetAllSellerMarketItemsByPubKeys(
                    userPubKey,
                    FMONE.Current.MarketPoolManager,
                    FMONE.Current.MarketBlockChainManager);

                var skynetHelper = new SkynetHelper();
                skynetHelper.PreloadTitlePhotos(myOffers, _logger);

                var myOffersActive = myOffers.Where(a => a.State == (int)MarketManager.ProductStateEnum.Default);
                var myOffersSold = myOffers.Where(a => a.State == (int)MarketManager.ProductStateEnum.Sold);
                if (myOffersSold.Any()) this.FindControl<TextBlock>("TBSoldProducts").IsVisible = true;

                DataContext = new MyProductsPageViewModel(myOffersActive, myOffersSold);
            }           
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

        public void ButtonAdd_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, AddEditProductPage.Instance);

            ClearForm();
        }

        public void ButtonBoughtProducts_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyBoughtProductsPage.Instance);

            ClearForm();
        }

        public void ButtonProduct_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            var signature = ((Button)sender).Tag.ToString();

            var marketItem = ((MyProductsPageViewModel)this.DataContext).Items.FirstOrDefault(a => a.Signature == signature);
            if ((marketItem != null) && (!marketItem.IsInPool))
            {
                var myProductItemPage = MyProductItemPage.Instance;
                myProductItemPage.LoadProduct(signature);

                PagesHelper.Switch(mainWindow, myProductItemPage);
            }
        }

        public void ButtonSoldProduct_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            var signature = ((Button)sender).Tag.ToString();

            var marketItem = ((MyProductsPageViewModel)this.DataContext).Items.FirstOrDefault(a => a.Signature == signature);
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
