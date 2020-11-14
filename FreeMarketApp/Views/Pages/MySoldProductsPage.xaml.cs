using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketOne.Markets;
using FreeMarketOne.Search;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class MySoldProductsPage : UserControl
    {
        private static MySoldProductsPage _instance;
        private ILogger _logger;
        private static int selectedPageSize = 20;
        private bool _initialized = false;
        private List<Selector> _appliedFilters = new List<Selector>();

        
        public static MySoldProductsPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MySoldProductsPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static MySoldProductsPage GetInstance()
        {
            return _instance;
        }

        public MySoldProductsPage():this(true)
        {

        }

        public MySoldProductsPage(bool fill = true)
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
                List<byte[]> list = new List<byte[]>();
                list.Add(userPubKey);

                var engine = FMONE.Current.SearchEngine;
                var result = engine.GetMyCompletedOffers(OfferDirection.Sold, selectedPageSize, 1);

                var skynetHelper = new SkynetHelper();
                skynetHelper.PreloadTitlePhotos(result.Results, _logger);
                DataContext = new MyProductsPageViewModel(result, _appliedFilters);
            }

            SetPageSizeOnControl(selectedPageSize);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _initialized = true;
        }

        private void SetPageSizeOnControl(int pageSize)
        {
            if (!_initialized) return;

            var cbPageSize = this.FindControl<ComboBox>("CBPageSize");
            cbPageSize.SelectedItem = cbPageSize.Items.OfType<ComboBoxItem>().Single(t => t.Content.Equals(pageSize.ToString()));
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyProductsPage.Instance);

            ClearForm();
        }


        public void ButtonBoughtProducts_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MyBoughtProductsPage.Instance);

            ClearForm();
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

        public void OnPageSize_Change(object sender, SelectionChangedEventArgs e)
        {

            int thisPageSize = selectedPageSize;

            string selection = ((Avalonia.Controls.ContentControl)((Avalonia.Controls.Primitives.SelectingItemsControl)sender).SelectedItem).Content.ToString();
            if (int.TryParse(selection, out thisPageSize) && !thisPageSize.Equals(selectedPageSize))
            {
                if (!_initialized) return;// this is just false signal by app setting to expected value.

                var engine = FMONE.Current.SearchEngine;
                engine.PageSize = thisPageSize;
                selectedPageSize = thisPageSize;

                var currentSearchResult = ((MyProductsPageViewModel)this.DataContext).Result;
                var result = engine.GetMyCompletedOffers(OfferDirection.Sold, selectedPageSize, currentSearchResult.CurrentPage);

                var skynetHelper = new SkynetHelper();
                skynetHelper.PreloadTitlePhotos(result.Results, _logger);
                DataContext = new MyProductsPageViewModel(result, _appliedFilters);
            }
        }

        public void ButtonNextPage_Click(object sender, RoutedEventArgs args)
        {
            var engine = FMONE.Current.SearchEngine;
            var currentSearchResult = ((MyProductsPageViewModel)this.DataContext).Result;

            if (currentSearchResult.CurrentPage * currentSearchResult.PageSize < (currentSearchResult.TotalHits))
            {
                var result = engine.GetMyCompletedOffers(OfferDirection.Sold, selectedPageSize, currentSearchResult.CurrentPage + 1);

                var skynetHelper = new SkynetHelper();
                skynetHelper.PreloadTitlePhotos(result.Results, _logger);
                DataContext = new MyProductsPageViewModel(result, _appliedFilters);
            }

        }

        public void ButtonPreviousPage_Click(object sender, RoutedEventArgs args)
        {
            var engine = FMONE.Current.SearchEngine;
            var currentSearchResult = ((MyProductsPageViewModel)this.DataContext).Result;

            if (currentSearchResult.CurrentPage > 1)
            {
                var result = engine.GetMyCompletedOffers(OfferDirection.Sold, selectedPageSize, currentSearchResult.CurrentPage - 1);
                var skynetHelper = new SkynetHelper();
                skynetHelper.PreloadTitlePhotos(result.Results, _logger);
                DataContext = new MyProductsPageViewModel(result, _appliedFilters);
            }
        }

        private void ClearForm()
        {
            //_instance = new MySoldProductsPage(false);
        }
    }
}
