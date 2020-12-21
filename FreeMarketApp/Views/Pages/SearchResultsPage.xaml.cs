using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketOne.Search;
using Lucene.Net.Search;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FMONE = FreeMarketOne.ServerCore.FreeMarketOneServer;

namespace FreeMarketApp.Views.Pages
{
    public class SearchResultsPage : UserControl
    {
        private static SearchResultsPage _instance;
        private ILogger _logger;
        private static string _searchPhrase;
        private static List<Selector> _appliedFilters = new List<Selector>();
        private static int selectedPageSize = 5;
        private bool _initialized = false;
        private SkynetHelper _skynetHelper;
        public static SearchResultsPage Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SearchResultsPage();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public static void SetSearchPhrase(string searchPhrase = null)
        {
            _searchPhrase = searchPhrase;
            _appliedFilters = new List<Selector>();
            //_appliedFilters.Add(new Selector("Sold", "No"));
        }

        public static SearchResultsPage GetInstance()
        {
            return _instance;
        }

        public static void ResetInstance()
        {
            _instance = null;
            _appliedFilters = new List<Selector>();
            //_appliedFilters.Add(new Selector("Sold", "No"));
        }

        public SearchResultsPage()
        {
            if (FMONE.Current.Logger != null)
                _logger = FMONE.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(SearchResultsPage).Namespace, typeof(SearchResultsPage).Name));

            if ((FMONE.Current.MarketManager != null) && (FMONE.Current.UserManager != null))
            {
                SpinWait.SpinUntil(() => FMONE.Current.GetServerState() == FMONE.FreeMarketOneServerStates.Online);
                
                _skynetHelper = new SkynetHelper();
                GetAndRenderQueryResults();
            }

            this.InitializeComponent();

            SetPageSizeOnControl(selectedPageSize);
        }

        private void GetAndRenderQueryResults()
        {
            PagesHelper.Log(_logger, string.Format("Loading search results from lucene."));

            var engine = FMONE.Current.SearchEngine;
            engine.PageSize = selectedPageSize;

            //var result = engine.Search(_searchPhrase);

            //this has page level query and will allow filter management here
            var query = engine.ParseQuery(_searchPhrase);

            //compose drildown with accumulated filters in page level list instead of queries
            Query newQuery = engine.BuildDrillDown(_appliedFilters, query);

            var result = engine.Search(newQuery, true, 1);

            _skynetHelper.PreloadTitlePhotos(result.Results, _logger);

            DataContext = new SearchResultsPageViewModel(result, _appliedFilters);

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            _initialized = true;
        }

        public void ButtonBack_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, MainPage.Instance);
        }

        public void ButtonProduct_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);
            var signature = ((Button)sender).Tag.ToString();

            var productPage = ProductPage.Instance;
            productPage.LoadProduct(signature);
            PagesHelper.Switch(mainWindow, productPage);
        }

        public void OnPageSize_Change(object sender, SelectionChangedEventArgs e)
        {
            int thisPageSize = selectedPageSize;

            string selection = ((ContentControl)((Avalonia.Controls.Primitives.SelectingItemsControl)sender).SelectedItem).Content.ToString();
            if (int.TryParse(selection, out thisPageSize) && !thisPageSize.Equals(selectedPageSize))
            {
                if (!_initialized) return;// this is just false signal by app setting to expected value.

                var engine = FMONE.Current.SearchEngine;
                engine.PageSize = thisPageSize;
                selectedPageSize = thisPageSize;

                var currentSearchResult = ((SearchResultsPageViewModel)this.DataContext).Result;
                var result = engine.Search(currentSearchResult.CurrentQuery, true, 1);

                _skynetHelper.PreloadTitlePhotos(result.Results, _logger);
                DataContext = new SearchResultsPageViewModel(result, _appliedFilters);
            } 
        }

        public void ButtonFacet_Click(object sender, RoutedEventArgs args)
        {
            var p3 = ((Button)sender).Parent.Parent.InteractiveParent;
            var category = ((StackPanel)p3).Tag.ToString();
            var filter = ((Button)sender).Tag.ToString();

            Selector selector = new Selector(category, filter);

            var targetItem = _appliedFilters.Find(i => i.Name == selector.Name && i.Value == selector.Value);
            if (targetItem == null)
            {
                _appliedFilters.Add(selector);
            }
            else
            {
                _appliedFilters.Remove(targetItem);
            }
            FilterList();
        }

        public static bool ValidateQuery(string query)
        {
            return FMONE.Current.SearchEngine.ValidateQuery(query);
        }

        private void FilterList()
        {
            var engine = FMONE.Current.SearchEngine;
            var currentSearchResult = ((SearchResultsPageViewModel)this.DataContext).Result;

            //current query has currently applied filters embeded, but also has page position and other parameters.
            //var currentQuery = currentSearchResult.CurrentQuery;

            //this has page level query and will allow filter management here
            var query = engine.ParseQuery(_searchPhrase);

            //compose drildown with accumulated filters in page level list instead of queries
            Query newQuery = engine.BuildDrillDown(_appliedFilters, query);

            var result = engine.Search(newQuery, true, currentSearchResult.CurrentPage);

            _skynetHelper.PreloadTitlePhotos(result.Results, _logger);
            DataContext = new SearchResultsPageViewModel(result, _appliedFilters);
        }

        private void SetPageSizeOnControl(int pageSize)
        {
            if (!_initialized) return;

            var cbPageSize = this.FindControl<ComboBox>("CBPageSize");
            cbPageSize.SelectedItem = cbPageSize.Items.OfType<ComboBoxItem>().Single(t => t.Content.Equals(pageSize.ToString()));
        }

        public void ButtonResetAllFacets_Click(object sender, RoutedEventArgs args)
        {
            _appliedFilters = new List<Selector>();
            FilterList();
        }

        public void ButtonResetFacet_Click(object sender, RoutedEventArgs args)
        {
            string unselectedFilterName = ((Button)sender).Tag.ToString();
            List<Selector> toBeRemoved = new List<Selector>();
            foreach (var item in _appliedFilters)
            {
                if (item.Name == unselectedFilterName)
                {
                    //removing item directly kills enumerator and throws exception
                    //_appliedFilters.Remove(item);
                    toBeRemoved.Add(item);
                    //break; must not break as could be more than one facet per given dimention
                }
            }
            foreach (var item in toBeRemoved)
            {
                _appliedFilters.Remove(item);
            }
           
            FilterList();
        }

        public void ButtonNextPage_Click(object sender, RoutedEventArgs args)
        {
            var engine = FMONE.Current.SearchEngine;
            var currentSearchResult = ((SearchResultsPageViewModel)this.DataContext).Result;

            if (currentSearchResult.CurrentPage * currentSearchResult.PageSize < (currentSearchResult.TotalHits))
            {
                //current query has currently applied filters embeded, but also has page position and other parameters.
                var currentQuery = currentSearchResult.CurrentQuery;

                var result = engine.Search(currentQuery, true, currentSearchResult.CurrentPage + 1);

                _skynetHelper.PreloadTitlePhotos(result.Results, _logger);
                DataContext = new SearchResultsPageViewModel(result, _appliedFilters);
            }
            
        }

        public void ButtonPreviousPage_Click(object sender, RoutedEventArgs args)
        {
            var engine = FMONE.Current.SearchEngine;
            var currentSearchResult = ((SearchResultsPageViewModel)this.DataContext).Result;

            if (currentSearchResult.CurrentPage > 1)
            {
                //current query has currently applied filters embeded, but also has page position and other parameters.
                var currentQuery = currentSearchResult.CurrentQuery;
                var result = engine.Search(currentQuery, true, currentSearchResult.CurrentPage - 1);
                _skynetHelper.PreloadTitlePhotos(result.Results, _logger);
                DataContext = new SearchResultsPageViewModel(result, _appliedFilters);
            }
        }

    }
}
