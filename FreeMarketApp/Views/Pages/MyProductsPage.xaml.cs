using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketApp.Views.Controls;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.ServerCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MyProductsPage).Namespace, typeof(MyProductsPage).Name));
            this.InitializeComponent();

            if ((FreeMarketOneServer.Current.MarketManager != null) && (FreeMarketOneServer.Current.UserManager != null))
            {
                SpinWait.SpinUntil(() => FreeMarketOneServer.Current.GetServerState() == FreeMarketOneServer.FreeMarketOneServerStates.Online);

                PagesHelper.Log(_logger, string.Format("Loading my market offers from chain."));

                var userPubKey = FreeMarketOneServer.Current.UserManager.GetCurrentUserPublicKey();

                //my own offers or sells
                var myOffers = FreeMarketOneServer.Current.MarketManager.GetAllSellerMarketItemsByPubKeys(userPubKey);

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
