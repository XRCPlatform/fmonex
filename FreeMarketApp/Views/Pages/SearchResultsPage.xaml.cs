using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FreeMarketApp.Helpers;
using FreeMarketApp.ViewModels;
using FreeMarketOne.Search;
using FreeMarketOne.ServerCore;
using Lucene.Net.Search;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FreeMarketApp.Views.Pages
{
    public class SearchResultsPage : UserControl
    {
        private static SearchResultsPage _instance;
        private ILogger _logger;
        private static string _searchPhrase;
        private static List<Selector> _appliedFilters = new List<Selector>();

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
        }

        public static SearchResultsPage GetInstance()
        {
            return _instance;
        }

        public static void ResetInstance()
        {
            _instance = null;
            _appliedFilters = new List<Selector>();
        }

        public SearchResultsPage()
        {
            if (FreeMarketOneServer.Current.Logger != null)
                _logger = FreeMarketOneServer.Current.Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName,
                            string.Format("{0}.{1}", typeof(SearchResultsPage).Namespace, typeof(SearchResultsPage).Name));

            if ((FreeMarketOneServer.Current.MarketManager != null) && (FreeMarketOneServer.Current.UserManager != null))
            {
                SpinWait.SpinUntil(() => FreeMarketOneServer.Current.GetServerState() == FreeMarketOneServer.FreeMarketOneServerStates.Online);

                GetAndRenderQueryResults();
            }

            this.InitializeComponent();
        }

        private void GetAndRenderQueryResults()
        {
            PagesHelper.Log(_logger, string.Format("Loading search results from lucene."));

            var engine = FreeMarketOneServer.Current.SearchEngine;
            var result = engine.Search(_searchPhrase);

            SkynetHelper.PreloadTitlePhotos(result.Results, _logger);

            DataContext = new SearchResultsPageViewModel(result);
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

        public void ButtonProduct_Click(object sender, RoutedEventArgs args)
        {
            var mainWindow = PagesHelper.GetParentWindow(this);

            PagesHelper.Switch(mainWindow, ProductPage.Instance);
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

        private void FilterList()
        {
            var engine = FreeMarketOneServer.Current.SearchEngine;
            var currentSearchResult = ((SearchResultsPageViewModel)this.DataContext).Result;
            
            //current query has currently applied filters embeded, but also has page position and other parameters.
            //var currentQuery = currentSearchResult.CurrentQuery;
            
            //this has page level query and will allow filter management here
            var query = engine.ParseQuery(_searchPhrase);          

            //compose drildown with accumulated filters in page level list instead of queries
            Query newQuery = engine.BuildDrillDown(_appliedFilters, query);

            var result = engine.Search(newQuery, true, currentSearchResult.CurrentPage);

            SkynetHelper.PreloadTitlePhotos(result.Results, _logger);
            DataContext = new SearchResultsPageViewModel(result);

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

    }
}
