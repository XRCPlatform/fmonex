using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Markets;
using FreeMarketOne.Search;
using Lucene.Net.Search;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class MainPage : UserControl
    {
        private static MainPage _instance;
        private ILogger _logger;
        private static List<Selector> _appliedFilters = new List<Selector>();
        private static int selectedPageSize = 5;
        private bool _initialized = false;
        public ObservableCollection<MarketItemV1> Items { get; }

        public static MainPage Instance 
        {
            get
            {
                if (_instance == null)
                    _instance = new MainPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static MainPage GetInstance()
        {
            return _instance;
        }

        public MainPage()
        {
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(MainPage).Namespace, typeof(MainPage).Name));

            this.InitializeComponent();

            if (FMONE.Current.Markets != null)
            {
                SpinWait.SpinUntil(() => FMONE.Current.GetServerState() == FMONE.FreeMarketOneServerStates.Online);

                PagesHelper.Log(_logger, string.Format("Loading market offers from chain."));

                var engine = FMONE.Current.SearchEngine;
                engine.PageSize = selectedPageSize;

                var result = engine.Search("", true, 1);

                var skynetHelper = new SkynetHelper();
                skynetHelper.PreloadTitlePhotos(result.Results, _logger);
                DataContext = new MainPageViewModel(result, _appliedFilters);
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

        public void ButtonProduct_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var signature = ((Button) sender).Tag.ToString();

            var productPage = ProductPage.Instance;
            productPage.LoadProduct(signature);
            PagesHelper.Switch(mainWindow, productPage);
        }

        public void ButtonCategory_Click(object sender, RoutedEventArgs args)
        {
            
            var category = Enum.Parse<MarketManager.MarketCategoryEnum>(((Button)sender).Tag.ToString());

            _appliedFilters.Clear();
            if (category != MarketManager.MarketCategoryEnum.All)
            {
                _appliedFilters.Add(new Selector("Category", category.ToString()));
            }
            
            FilterList();

            var sidebarPanel = this.FindControl<StackPanel>("SideBar");
            if (sidebarPanel != null) {

                foreach (var item in sidebarPanel.Children)
                {
                    if (item is Button)
                    {
                        ((Button)item).Classes.Remove("sidebarSelected");
                        ((Button)item).Classes.Add("sidebar");
                    }
                }

                ((Button)sender).Classes.Add("sidebarSelected");
                ((Button)sender).Classes.Remove("sidebar");
            }
           
        }

        private void FilterList()
        {
            var engine = FMONE.Current.SearchEngine;
            engine.PageSize = selectedPageSize;

            Query newQuery = engine.BuildDrillDown(_appliedFilters, engine.ParseQuery(""));

            var result = engine.Search(newQuery, false, 1);

            var skynetHelper = new SkynetHelper();
            skynetHelper.PreloadTitlePhotos(result.Results, _logger);
            DataContext = new MainPageViewModel(result, _appliedFilters);
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

                var currentSearchResult = ((MainPageViewModel)this.DataContext).Result;
                var result = engine.Search(currentSearchResult.CurrentQuery, false, 1);

                var skynetHelper = new SkynetHelper();
                skynetHelper.PreloadTitlePhotos(result.Results, _logger);
                DataContext = new MainPageViewModel(result, _appliedFilters);
            }
        }

        public void ButtonNextPage_Click(object sender, RoutedEventArgs args)
        {
            var engine = FMONE.Current.SearchEngine;
            var currentSearchResult = ((MainPageViewModel)this.DataContext).Result;

            if (currentSearchResult.CurrentPage * currentSearchResult.PageSize < (currentSearchResult.TotalHits))
            {
                //current query has currently applied filters embeded, but also has page position and other parameters.
                var currentQuery = currentSearchResult.CurrentQuery;

                var result = engine.Search(currentQuery, false, currentSearchResult.CurrentPage + 1);

                var skynetHelper = new SkynetHelper();
                skynetHelper.PreloadTitlePhotos(result.Results, _logger);
                DataContext = new MainPageViewModel(result, _appliedFilters);
            }

        }

        public void ButtonPreviousPage_Click(object sender, RoutedEventArgs args)
        {
            var engine = FMONE.Current.SearchEngine;
            var currentSearchResult = ((MainPageViewModel)this.DataContext).Result;

            if (currentSearchResult.CurrentPage > 1)
            {
                //current query has currently applied filters embeded, but also has page position and other parameters.
                var currentQuery = currentSearchResult.CurrentQuery;
                var result = engine.Search(currentQuery, false, currentSearchResult.CurrentPage - 1);

                var skynetHelper = new SkynetHelper();
                skynetHelper.PreloadTitlePhotos(result.Results, _logger);
                DataContext = new MainPageViewModel(result, _appliedFilters);
            }
        }

        public void ButtonFilters_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, SearchResultsPage.Instance);
        }
    }
}
